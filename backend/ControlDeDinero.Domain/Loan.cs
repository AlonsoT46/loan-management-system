using System.Globalization;

namespace ControlDeDinero.Domain;

/// <summary>
/// A loan whose interest is a percentage of the principal for every quincena (15 days)
/// between the loan date and the due date. The derived amounts (interest, total, paid,
/// remaining) are only changed through <see cref="Create"/> and <see cref="RegisterPayment"/>,
/// so they cannot drift out of sync with the recorded payments.
/// </summary>
public class Loan
{
    public const decimal DefaultInterestRatePerQuincenaPercent = 20m;
    public const int DaysPerQuincena = 15;

    private Loan() { } // EF Core

    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid ClientId { get; private set; }

    public decimal PrincipalAmount { get; private set; }
    public decimal InterestRatePerQuincena { get; private set; }
    public int Quincenas { get; private set; }
    public decimal InterestAmount { get; private set; }
    public decimal TotalAmount { get; private set; }

    public DateTime LoanDate { get; private set; }
    public DateTime DueDate { get; private set; }

    public decimal TotalPaid { get; private set; }
    public decimal RemainingBalance { get; private set; }

    public LoanStatus Status { get; private set; } = LoanStatus.Active;
    public string? Notes { get; set; }

    public Client? Client { get; private set; }
    public ICollection<Installment> Installments { get; private set; } = new List<Installment>();
    public ICollection<Payment> Payments { get; private set; } = new List<Payment>();
    public ICollection<AuditEvent> AuditEvents { get; private set; } = new List<AuditEvent>();

    public static Loan Create(
        Guid clientId,
        decimal principalAmount,
        DateTime loanDate,
        DateTime dueDate,
        decimal interestRatePerQuincenaPercent = DefaultInterestRatePerQuincenaPercent,
        string? notes = null)
    {
        if (clientId == Guid.Empty)
            throw new ArgumentException("A loan needs a client.", nameof(clientId));

        principalAmount = RoundMoney(principalAmount);
        if (principalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(principalAmount), "Principal must be greater than zero.");

        if (interestRatePerQuincenaPercent < 0)
            throw new ArgumentOutOfRangeException(nameof(interestRatePerQuincenaPercent), "Interest rate cannot be negative.");

        if (dueDate < loanDate)
            throw new ArgumentException("Due date cannot be before the loan date.", nameof(dueDate));

        var quincenas = CountQuincenas(loanDate, dueDate);
        var interest = RoundMoney(principalAmount * interestRatePerQuincenaPercent / 100m * quincenas);
        var total = principalAmount + interest;

        var loan = new Loan
        {
            ClientId = clientId,
            PrincipalAmount = principalAmount,
            InterestRatePerQuincena = interestRatePerQuincenaPercent,
            Quincenas = quincenas,
            InterestAmount = interest,
            TotalAmount = total,
            RemainingBalance = total,
            LoanDate = loanDate,
            DueDate = dueDate,
            Notes = notes
        };

        loan.AuditEvents.Add(new AuditEvent(
            loan.Id,
            "LoanCreated",
            "Loan created: principal " + Format(principalAmount) + ", " + quincenas + " quincena(s), interest " + Format(interest) + "."));

        return loan;
    }

    /// <summary>
    /// Records a payment and splits it between principal and interest in proportion to the
    /// loan totals. The final payment absorbs any rounding difference. Requires
    /// <see cref="Payments"/> (with their allocations) to be loaded.
    /// </summary>
    public Payment RegisterPayment(decimal amount, DateTime paymentDate, string? notes = null)
    {
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException("Only active loans accept payments.");

        amount = RoundMoney(amount);
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Payment must be greater than zero.");

        if (paymentDate.Date < LoanDate.Date)
            throw new ArgumentException("Payment date cannot be before the loan date.", nameof(paymentDate));

        if (amount > RemainingBalance)
            throw new InvalidOperationException("Payment exceeds the remaining balance.");

        var allocations = Payments.SelectMany(p => p.Allocations).ToList();
        var remainingPrincipal = PrincipalAmount - allocations.Sum(a => a.PrincipalPaid);
        var remainingInterest = InterestAmount - allocations.Sum(a => a.InterestPaid);

        decimal principalPaid;
        decimal interestPaid;

        if (amount == RemainingBalance)
        {
            principalPaid = remainingPrincipal;
            interestPaid = remainingInterest;
        }
        else
        {
            principalPaid = Math.Min(RoundMoney(amount * PrincipalAmount / TotalAmount), remainingPrincipal);
            interestPaid = amount - principalPaid;

            if (interestPaid > remainingInterest)
            {
                interestPaid = remainingInterest;
                principalPaid = amount - interestPaid;
            }
        }

        var payment = new Payment(Id, paymentDate, amount, notes);
        payment.Allocate(principalPaid, interestPaid);
        Payments.Add(payment);

        TotalPaid += amount;
        RemainingBalance -= amount;

        if (RemainingBalance == 0m)
            Status = LoanStatus.Paid;

        AuditEvents.Add(new AuditEvent(
            Id,
            "PaymentRegistered",
            "Payment " + Format(amount) + " on " + paymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            + " (principal " + Format(principalPaid) + ", interest " + Format(interestPaid) + ")."));

        return payment;
    }

    /// <summary>
    /// Number of quincenas (15-day periods, rounded up, minimum 1) between two dates.
    /// Day 1 to day 14 is 1 quincena; day 1 to day 30 is 2.
    /// </summary>
    public static int CountQuincenas(DateTime loanDate, DateTime dueDate)
    {
        var days = (dueDate.Date - loanDate.Date).Days;
        return Math.Max(1, (days + DaysPerQuincena - 1) / DaysPerQuincena);
    }

    /// <summary>
    /// A loan is overdue when it still has a balance and its due date has passed.
    /// Derived on purpose (never stored): it would go stale every day. Passing the due
    /// date does not add more quincenas or interest.
    /// </summary>
    public bool IsOverdue(DateTime asOf) =>
        Status == LoanStatus.Active && RemainingBalance > 0m && asOf.Date > DueDate.Date;

    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string Format(decimal value) =>
        value.ToString("F2", CultureInfo.InvariantCulture);
}
