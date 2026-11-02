using ControlDeDinero.Domain;

namespace ControlDeDinero.Tests;

public class LoanTests
{
    private static readonly DateTime Today = new(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);

    // Default: 14 days = 1 quincena, 20% per quincena.
    private static Loan NewLoan(decimal principal = 50_000m, decimal ratePercent = 20m, int days = 14) =>
        Loan.Create(Guid.NewGuid(), principal, Today, Today.AddDays(days), ratePercent);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 1)]
    [InlineData(14, 1)]
    [InlineData(15, 1)]
    [InlineData(16, 2)]
    [InlineData(29, 2)]
    [InlineData(30, 2)]
    [InlineData(31, 3)]
    public void CountQuincenas_RoundsUpFifteenDayPeriods(int days, int expected)
    {
        Assert.Equal(expected, Loan.CountQuincenas(Today, Today.AddDays(days)));
    }

    [Fact]
    public void Create_OneQuincena_AddsTwentyPercent()
    {
        var loan = NewLoan(50_000m);

        Assert.Equal(1, loan.Quincenas);
        Assert.Equal(10_000m, loan.InterestAmount);
        Assert.Equal(60_000m, loan.TotalAmount);
        Assert.Equal(60_000m, loan.RemainingBalance);
        Assert.Equal(0m, loan.TotalPaid);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void Create_FromTheFirstToTheThirtieth_IsTwoQuincenas()
    {
        // 50,000 borrowed on the 1st and repaid on the 30th => 70,000.
        var loan = Loan.Create(Guid.NewGuid(), 50_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 30));

        Assert.Equal(2, loan.Quincenas);
        Assert.Equal(20_000m, loan.InterestAmount);
        Assert.Equal(70_000m, loan.TotalAmount);
    }

    [Theory]
    [InlineData(10_000, 2_000)]
    [InlineData(50_000, 10_000)]
    [InlineData(100_000, 20_000)]
    public void Create_DefaultRateIsTwentyPercentPerQuincena(int principal, int expectedInterest)
    {
        var loan = Loan.Create(Guid.NewGuid(), principal, Today, Today.AddDays(14));

        Assert.Equal(20m, loan.InterestRatePerQuincena);
        Assert.Equal(expectedInterest, loan.InterestAmount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Create_RejectsNonPositivePrincipal(int principal)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => NewLoan(principal));
    }

    [Fact]
    public void Create_RejectsDueDateBeforeLoanDate()
    {
        Assert.Throws<ArgumentException>(() =>
            Loan.Create(Guid.NewGuid(), 1_000m, Today, Today.AddDays(-1)));
    }

    [Fact]
    public void RegisterPayment_SplitsProportionallyBetweenPrincipalAndInterest()
    {
        var loan = NewLoan(50_000m); // total 60,000

        var payment = loan.RegisterPayment(12_000m, Today.AddDays(10));

        var allocation = Assert.Single(payment.Allocations);
        Assert.Equal(10_000m, allocation.PrincipalPaid);
        Assert.Equal(2_000m, allocation.InterestPaid);
        Assert.Equal(12_000m, loan.TotalPaid);
        Assert.Equal(48_000m, loan.RemainingBalance);
        Assert.Equal(LoanStatus.Active, loan.Status);
    }

    [Fact]
    public void RegisterPayment_FinalPaymentAbsorbsRoundingAndClosesLoan()
    {
        var loan = NewLoan(100m, 10m); // interest 10.00, total 110.00

        loan.RegisterPayment(33.33m, Today.AddDays(1));
        loan.RegisterPayment(33.33m, Today.AddDays(2));
        loan.RegisterPayment(43.34m, Today.AddDays(3));

        var allocations = loan.Payments.SelectMany(p => p.Allocations).ToList();
        Assert.Equal(100m, allocations.Sum(a => a.PrincipalPaid));
        Assert.Equal(10m, allocations.Sum(a => a.InterestPaid));
        Assert.Equal(0m, loan.RemainingBalance);
        Assert.Equal(110m, loan.TotalPaid);
        Assert.Equal(LoanStatus.Paid, loan.Status);
    }

    [Fact]
    public void RegisterPayment_RejectsOverpayment()
    {
        var loan = NewLoan(1_000m, 10m); // total 1,100

        Assert.Throws<InvalidOperationException>(() =>
            loan.RegisterPayment(1_100.01m, Today.AddDays(1)));
        Assert.Equal(1_100m, loan.RemainingBalance);
    }

    [Fact]
    public void RegisterPayment_RejectsPaymentOnPaidLoan()
    {
        var loan = NewLoan(1_000m, 10m);
        loan.RegisterPayment(1_100m, Today.AddDays(1));

        Assert.Throws<InvalidOperationException>(() =>
            loan.RegisterPayment(1m, Today.AddDays(2)));
    }

    [Fact]
    public void RegisterPayment_RejectsNonPositiveAmount()
    {
        var loan = NewLoan();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            loan.RegisterPayment(0m, Today.AddDays(1)));
    }

    [Fact]
    public void RegisterPayment_LeavesAnAuditTrail()
    {
        var loan = NewLoan();

        loan.RegisterPayment(1_000m, Today.AddDays(1));

        Assert.Contains(loan.AuditEvents, e => e.EventType == "LoanCreated");
        Assert.Contains(loan.AuditEvents, e => e.EventType == "PaymentRegistered");
    }

    [Fact]
    public void IsOverdue_OnlyAfterDueDateAndWhileThereIsABalance()
    {
        var loan = NewLoan(1_000m, 10m, days: 14); // due on Today + 14

        Assert.False(loan.IsOverdue(Today.AddDays(14)));
        Assert.True(loan.IsOverdue(Today.AddDays(15)));

        loan.RegisterPayment(1_100m, Today.AddDays(16));
        Assert.False(loan.IsOverdue(Today.AddDays(30)));
    }

    [Fact]
    public void IsOverdue_DoesNotAddInterest()
    {
        var loan = NewLoan(50_000m);

        Assert.True(loan.IsOverdue(Today.AddDays(60)));
        Assert.Equal(60_000m, loan.RemainingBalance);
    }

    [Fact]
    public void RegisterPayment_RejectsDateBeforeLoanDate()
    {
        var loan = NewLoan();

        Assert.Throws<ArgumentException>(() =>
            loan.RegisterPayment(1_000m, Today.AddDays(-1)));
    }

    [Fact]
    public void VoidPayment_RestoresBalanceAndReopensAPaidLoan()
    {
        var loan = NewLoan(1_000m, 10m); // total 1,100
        var payment = loan.RegisterPayment(1_100m, Today.AddDays(1));
        Assert.Equal(LoanStatus.Paid, loan.Status);

        loan.VoidPayment(payment.Id, DateTime.UtcNow, "wrong amount");

        Assert.True(payment.IsVoided);
        Assert.Equal("wrong amount", payment.VoidReason);
        Assert.NotNull(payment.VoidedAtUtc);
        Assert.Equal(0m, loan.TotalPaid);
        Assert.Equal(1_100m, loan.RemainingBalance);
        Assert.Equal(LoanStatus.Active, loan.Status);
        Assert.Contains(loan.AuditEvents, e => e.EventType == "PaymentVoided");
        Assert.Single(loan.Payments); // never deleted
    }

    [Fact]
    public void VoidPayment_RejectsVoidingTwice()
    {
        var loan = NewLoan(1_000m, 10m);
        var payment = loan.RegisterPayment(500m, Today.AddDays(1));
        loan.VoidPayment(payment.Id, DateTime.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            loan.VoidPayment(payment.Id, DateTime.UtcNow));
    }

    [Fact]
    public void VoidPayment_RejectsUnknownPayment()
    {
        var loan = NewLoan(1_000m, 10m);
        loan.RegisterPayment(500m, Today.AddDays(1));

        Assert.Throws<InvalidOperationException>(() =>
            loan.VoidPayment(Guid.NewGuid(), DateTime.UtcNow));
    }

    [Fact]
    public void RegisterPayment_IgnoresVoidedPaymentsWhenSplitting()
    {
        var loan = NewLoan(100m, 10m); // interest 10.00, total 110.00
        var first = loan.RegisterPayment(55m, Today.AddDays(1));
        loan.VoidPayment(first.Id, DateTime.UtcNow);

        loan.RegisterPayment(110m, Today.AddDays(2));

        var counted = loan.Payments.Where(p => !p.IsVoided).SelectMany(p => p.Allocations).ToList();
        Assert.Equal(100m, counted.Sum(a => a.PrincipalPaid));
        Assert.Equal(10m, counted.Sum(a => a.InterestPaid));
        Assert.Equal(LoanStatus.Paid, loan.Status);
    }

    [Fact]
    public void Cancel_MarksTheLoanCancelledAndKeepsAnAuditTrail()
    {
        var loan = NewLoan();

        loan.Cancel("registered by mistake");

        Assert.Equal(LoanStatus.Cancelled, loan.Status);
        Assert.Contains(loan.AuditEvents, e => e.EventType == "LoanCancelled");
    }

    [Fact]
    public void Cancel_RejectsALoanWithActivePayments()
    {
        var loan = NewLoan();
        loan.RegisterPayment(1_000m, Today.AddDays(1));

        Assert.Throws<InvalidOperationException>(() => loan.Cancel());
    }

    [Fact]
    public void Cancel_AllowsALoanWhoseOnlyPaymentsWereVoided()
    {
        var loan = NewLoan();
        var payment = loan.RegisterPayment(1_000m, Today.AddDays(1));
        loan.VoidPayment(payment.Id, DateTime.UtcNow);

        loan.Cancel();

        Assert.Equal(LoanStatus.Cancelled, loan.Status);
    }

    [Fact]
    public void Cancel_RejectsCancellingTwice_AndBlocksMorePayments()
    {
        var loan = NewLoan();
        loan.Cancel();

        Assert.Throws<InvalidOperationException>(() => loan.Cancel());
        Assert.Throws<InvalidOperationException>(() => loan.RegisterPayment(1_000m, Today.AddDays(1)));
    }
}
