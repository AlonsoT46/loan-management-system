using ControlDeDinero.Application;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Application.Summary;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Tests;

public class SummaryServiceTests
{
    // Today in Costa Rica is 2026-11-01.
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 2, 0, 0, TimeSpan.Zero);

    private readonly FakeClientRepository _clients = new();
    private readonly FakeLoanRepository _loans;

    public SummaryServiceTests()
    {
        _loans = new FakeLoanRepository(_clients);
    }

    private LoanService NewLoanService() => new(_loans, _clients, new FakeCurrentUser(), new FixedTimeProvider(Now));

    private SummaryService NewSummaryService() => new(_loans, new FakeCurrentUser(), new FixedTimeProvider(Now));

    private Guid AddClient()
    {
        var client = new Client { Id = Guid.NewGuid(), OwnerId = FakeCurrentUser.DefaultUserId, Name = "Ana" };
        _clients.Clients.Add(client);
        return client.Id;
    }

    /// <summary>
    /// Loan A: 50,000 from Oct 15 to Oct 29 (1 quincena, total 60,000), paid 12,000 on Oct 20
    /// (10,000 principal + 2,000 interest). It is overdue.
    /// Loan B: 100,000 from Nov 1 to Nov 14 (1 quincena, total 120,000), paid 24,000 on Nov 1
    /// (20,000 principal + 4,000 interest). Not overdue.
    /// </summary>
    private async Task<(LoanDetailDto A, LoanDetailDto B, Guid APaymentId)> SeedAsync()
    {
        var service = NewLoanService();
        var clientId = AddClient();

        var a = await service.CreateAsync(new CreateLoanRequest(
            clientId, 50_000m, new DateTime(2026, 10, 15), new DateTime(2026, 10, 29), null, null));
        var aPaid = await service.RegisterPaymentAsync(
            a.Loan.Id, new RegisterPaymentRequest(12_000m, new DateTime(2026, 10, 20), null));

        var b = await service.CreateAsync(new CreateLoanRequest(
            clientId, 100_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), null, null));
        await service.RegisterPaymentAsync(
            b.Loan.Id, new RegisterPaymentRequest(24_000m, new DateTime(2026, 11, 1), null));

        return (a, b, aPaid!.Payments[0].Id);
    }

    [Fact]
    public async Task AllTime_AddsUpEverything()
    {
        await SeedAsync();

        var summary = await NewSummaryService().GetAsync(null);

        Assert.Null(summary.Month);
        Assert.Equal(150_000m, summary.TotalLent);
        Assert.Equal(30_000m, summary.ExpectedInterest);  // 10,000 (A) + 20,000 (B)
        Assert.Equal(180_000m, summary.ExpectedTotal);
        Assert.Equal(30_000m, summary.PrincipalRecovered);
        Assert.Equal(6_000m, summary.InterestCollected);
        Assert.Equal(36_000m, summary.TotalCollected);
        Assert.Equal(144_000m, summary.OutstandingBalance); // 48,000 + 96,000
        Assert.Equal(2, summary.ActiveLoans);
        Assert.Equal(1, summary.OverdueLoans);
        Assert.Equal(48_000m, summary.OverdueBalance);
        Assert.Equal(96_000m, summary.DueThisMonth); // B is due Nov 14; A is overdue, so it is not here
        Assert.Equal(1, summary.DueThisMonthLoans);
        Assert.Equal(0, summary.ArchivedLoans);
    }

    [Fact]
    public async Task November_LimitsLentAndCollectedButNotTheBalances()
    {
        await SeedAsync();

        var summary = await NewSummaryService().GetAsync("2026-11");

        Assert.Equal("2026-11", summary.Month);
        Assert.Equal(100_000m, summary.TotalLent);
        Assert.Equal(20_000m, summary.ExpectedInterest);
        Assert.Equal(120_000m, summary.ExpectedTotal);
        Assert.Equal(20_000m, summary.PrincipalRecovered);
        Assert.Equal(4_000m, summary.InterestCollected);
        Assert.Equal(24_000m, summary.TotalCollected);
        Assert.Equal(144_000m, summary.OutstandingBalance);
        Assert.Equal(1, summary.OverdueLoans);
    }

    [Fact]
    public async Task October_ShowsOnlyOctoberActivity()
    {
        await SeedAsync();

        var summary = await NewSummaryService().GetAsync("2026-10");

        Assert.Equal(50_000m, summary.TotalLent);
        Assert.Equal(10_000m, summary.ExpectedInterest);
        Assert.Equal(60_000m, summary.ExpectedTotal);
        Assert.Equal(10_000m, summary.PrincipalRecovered);
        Assert.Equal(2_000m, summary.InterestCollected);
        Assert.Equal(12_000m, summary.TotalCollected);
    }

    [Fact]
    public async Task VoidedPayments_StopCounting()
    {
        var (a, _, paymentId) = await SeedAsync();
        await NewLoanService().VoidPaymentAsync(a.Loan.Id, paymentId, null);

        var summary = await NewSummaryService().GetAsync(null);

        Assert.Equal(20_000m, summary.PrincipalRecovered);
        Assert.Equal(4_000m, summary.InterestCollected);
        Assert.Equal(156_000m, summary.OutstandingBalance); // 60,000 + 96,000
        Assert.Equal(60_000m, summary.OverdueBalance);
    }

    [Fact]
    public async Task CancelledLoans_AreLeftOut()
    {
        await SeedAsync();
        var service = NewLoanService();
        var mistake = await service.CreateAsync(new CreateLoanRequest(
            AddClient(), 10_000m, new DateTime(2026, 11, 1), new DateTime(2026, 11, 14), null, null));
        await service.CancelAsync(mistake.Loan.Id, null);

        var summary = await NewSummaryService().GetAsync(null);

        Assert.Equal(150_000m, summary.TotalLent);
        Assert.Equal(2, summary.ActiveLoans);
    }

    [Fact]
    public async Task EmptyDatabase_GivesZeros()
    {
        var summary = await NewSummaryService().GetAsync(null);

        Assert.Equal(0m, summary.TotalLent);
        Assert.Equal(0m, summary.OutstandingBalance);
        Assert.Equal(0, summary.ActiveLoans);
        Assert.Equal(0, summary.OverdueLoans);
    }

    [Fact]
    public async Task RejectsABadMonth()
    {
        await Assert.ThrowsAsync<ValidationException>(() => NewSummaryService().GetAsync("2026-13"));
    }

    [Fact]
    public async Task Monthly_SplitsLentStillOutAndCollectedByMonth()
    {
        await SeedAsync();

        var points = await NewSummaryService().GetMonthlyAsync(2);

        Assert.Collection(points,
            october =>
            {
                Assert.Equal("2026-10", october.Month);
                Assert.Equal(50_000m, october.Lent);
                Assert.Equal(40_000m, october.PrincipalStillOut); // 50,000 - 10,000 recovered
                Assert.Equal(10_000m, october.PrincipalCollected);
                Assert.Equal(2_000m, october.InterestCollected);
            },
            november =>
            {
                Assert.Equal("2026-11", november.Month);
                Assert.Equal(100_000m, november.Lent);
                Assert.Equal(80_000m, november.PrincipalStillOut); // 100,000 - 20,000 recovered
                Assert.Equal(20_000m, november.PrincipalCollected);
                Assert.Equal(4_000m, november.InterestCollected);
            });
    }

    [Fact]
    public async Task Monthly_IgnoresVoidedPayments()
    {
        var (a, _, paymentId) = await SeedAsync();
        await NewLoanService().VoidPaymentAsync(a.Loan.Id, paymentId, null);

        var october = (await NewSummaryService().GetMonthlyAsync(2))[0];

        Assert.Equal(50_000m, october.PrincipalStillOut);
        Assert.Equal(0m, october.PrincipalCollected);
        Assert.Equal(0m, october.InterestCollected);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(25)]
    public async Task Monthly_RejectsAnInvalidNumberOfMonths(int months)
    {
        await Assert.ThrowsAsync<ValidationException>(() => NewSummaryService().GetMonthlyAsync(months));
    }

    [Fact]
    public async Task ArchivedLoan_StopsCountingAsMoneyToCollect()
    {
        var (a, _, _) = await SeedAsync();
        await NewLoanService().ArchiveAsync(a.Loan.Id, null);

        var summary = await NewSummaryService().GetAsync(null);
        Assert.Equal(150_000m, summary.TotalLent); // it was lent all the same
        Assert.Equal(96_000m, summary.OutstandingBalance); // only B
        Assert.Equal(0, summary.OverdueLoans);
        Assert.Equal(1, summary.ArchivedLoans);
        Assert.Equal(48_000m, summary.ArchivedBalance);

        var october = (await NewSummaryService().GetMonthlyAsync(2))[0];
        Assert.Equal(0m, october.PrincipalStillOut);
        Assert.Equal(40_000m, october.PrincipalWrittenOff); // 50,000 lent - 10,000 recovered
    }
}
