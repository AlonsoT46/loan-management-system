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
    /// Corrects a loan registered with a mistake: client, principal or dates. Interest and total are
    /// calculated again with the loan's own rate. Only while no payment counts: the split of a payment
    /// between principal and interest depends on these amounts, so changing them afterwards would
    /// leave the past payments wrong. To correct a loan with payments, void them first.
    /// </summary>
    public void UpdateTerms(Guid clientId, decimal principalAmount, DateTime loanDate, DateTime dueDate)
    {
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException("Only active loans can be edited.");

        if (Payments.Any(p => !p.IsVoided))
            throw new InvalidOperationException("A loan with payments cannot change its amounts or dates. Void the payments first.");

        if (clientId == Guid.Empty)
            throw new ArgumentException("A loan needs a client.", nameof(clientId));

        principalAmount = RoundMoney(principalAmount);
        if (principalAmount <= 0)
            throw new ArgumentOutOfRangeException(nameof(principalAmount), "Principal must be greater than zero.");

        if (dueDate < loanDate)
            throw new ArgumentException("Due date cannot be before the loan date.", nameof(dueDate));

        var before = Describe();

        var quincenas = CountQuincenas(loanDate, dueDate);
        var interest = RoundMoney(principalAmount * InterestRatePerQuincena / 100m * quincenas);

        ClientId = clientId;
        PrincipalAmount = principalAmount;
        LoanDate = loanDate;
        DueDate = dueDate;
        Quincenas = quincenas;
        InterestAmount = interest;
        TotalAmount = principalAmount + interest;
        TotalPaid = 0m; // no payment counts (checked above)
        RemainingBalance = TotalAmount;

        AuditEvents.Add(new AuditEvent(Id, "LoanEdited", "Loan edited. Before: " + before + ". After: " + Describe() + "."));
    }

    private string Describe() =>
        "principal " + Format(PrincipalAmount)
        + ", " + LoanDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        + " to " + DueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
        + ", total " + Format(TotalAmount);

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

        var allocations = Payments.Where(p => !p.IsVoided).SelectMany(p => p.Allocations).ToList();
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
    /// Voids a payment without deleting it: the payment stays in the history, flagged, and the
    /// balance is recalculated from the payments that still count. A paid loan becomes active again.
    /// </summary>
    public Payment VoidPayment(Guid paymentId, DateTime voidedAtUtc, string? reason = null)
    {
        if (Status == LoanStatus.Cancelled)
            throw new InvalidOperationException("A cancelled loan cannot be changed.");

        var payment = Payments.FirstOrDefault(p => p.Id == paymentId)
            ?? throw new InvalidOperationException("Payment not found in this loan.");

        if (payment.IsVoided)
            throw new InvalidOperationException("The payment is already voided.");

        payment.Void(voidedAtUtc, reason);

        TotalPaid = Payments.Where(p => !p.IsVoided).Sum(p => p.Amount);
        RemainingBalance = TotalAmount - TotalPaid;
        // An archived loan stays archived: voiding a payment does not bring it back by itself.
        if (Status != LoanStatus.Archived)
            Status = RemainingBalance == 0m ? LoanStatus.Paid : LoanStatus.Active;

        AuditEvents.Add(new AuditEvent(
            Id,
            "PaymentVoided",
            "Payment " + Format(payment.Amount) + " from "
            + payment.PaymentDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + " voided"
            + (reason is null ? "." : ": " + reason)));

        return payment;
    }

    /// <summary>
    /// Cancels the loan (for example, it was registered by mistake). It is kept in the history.
    /// A loan with payments cannot be cancelled until those payments are voided.
    /// </summary>
    public void Cancel(string? reason = null)
    {
        if (Status == LoanStatus.Cancelled)
            throw new InvalidOperationException("The loan is already cancelled.");

        if (Payments.Any(p => !p.IsVoided))
            throw new InvalidOperationException("Void the recorded payments first: a loan with payments cannot be cancelled.");

        Status = LoanStatus.Cancelled;

        AuditEvents.Add(new AuditEvent(
            Id,
            "LoanCancelled",
            reason is null ? "Loan cancelled." : "Loan cancelled: " + reason));
    }

    /// <summary>
    /// Archives ("writes off") an active loan the lender no longer expects to recover. It stops
    /// counting as money to collect and stops accepting payments, but stays in the history and can
    /// be unarchived (for example, if the client starts paying again).
    /// </summary>
    public void Archive(string? reason = null)
    {
        if (Status != LoanStatus.Active)
            throw new InvalidOperationException("Only active loans can be archived.");

        Status = LoanStatus.Archived;

        AuditEvents.Add(new AuditEvent(
            Id,
            "LoanArchived",
            reason is null ? "Loan archived (written off)." : "Loan archived (written off): " + reason));
    }

    public void Unarchive()
    {
        if (Status != LoanStatus.Archived)
            throw new InvalidOperationException("Only archived loans can be unarchived.");

        Status = RemainingBalance == 0m ? LoanStatus.Paid : LoanStatus.Active;

        AuditEvents.Add(new AuditEvent(Id, "LoanUnarchived", "Loan unarchived."));
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
