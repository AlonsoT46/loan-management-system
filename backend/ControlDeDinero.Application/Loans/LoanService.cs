using ControlDeDinero.Application.Clients;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Loans;

public class LoanService
{
    public const int MaxNotesLength = 500;

    private readonly ILoanRepository _loans;
    private readonly IClientRepository _clients;
    private readonly TimeProvider _time;

    public LoanService(ILoanRepository loans, IClientRepository clients, TimeProvider time)
    {
        _loans = loans;
        _clients = clients;
        _time = time;
    }

    public async Task<LoanDetailDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(request.ClientId, cancellationToken)
            ?? throw new ValidationException("Client not found.");

        if (!client.IsActive)
            throw new ValidationException("The client is inactive.");

        if (request.DueDate is null)
            throw new ValidationException("Due date is required.");

        var notes = CleanNotes(request.Notes);
        var today = Today();
        var loanDate = (request.LoanDate ?? today).Date;

        Loan loan;
        try
        {
            loan = Loan.Create(
                client.Id,
                request.PrincipalAmount,
                loanDate,
                request.DueDate.Value.Date,
                request.InterestRatePerQuincena ?? Loan.DefaultInterestRatePerQuincenaPercent,
                notes);
        }
        catch (ArgumentException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.AddAsync(loan, cancellationToken);
        await _loans.SaveChangesAsync(cancellationToken);

        return ToDetail(loan, client.Name, today);
    }

    public async Task<LoanDetailDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(id, cancellationToken);
        if (loan is null)
            return null;

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    public async Task<IReadOnlyList<LoanSummaryDto>> SearchAsync(
        Guid? clientId,
        LoanStatus? status,
        bool onlyOverdue,
        CancellationToken cancellationToken = default)
    {
        var today = Today();
        var loans = await _loans.SearchAsync(clientId, status, onlyOverdue ? today : null, cancellationToken);

        return loans
            .Select(l => ToSummary(l, l.Client?.Name ?? string.Empty, today))
            .ToList();
    }

    public async Task<LoanDetailDto?> RegisterPaymentAsync(
        Guid loanId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(loanId, cancellationToken);
        if (loan is null)
            return null;

        var today = Today();
        var paymentDate = (request.PaymentDate ?? today).Date;

        try
        {
            loan.RegisterPayment(request.Amount, paymentDate, CleanNotes(request.Notes));
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, today);
    }

    /// <summary>Today's date in the configured time zone (Costa Rica in production).</summary>
    private DateTime Today() => _time.GetLocalNow().Date;

    private static string? CleanNotes(string? notes)
    {
        var clean = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (clean is not null && clean.Length > MaxNotesLength)
            throw new ValidationException($"Notes cannot be longer than {MaxNotesLength} characters.");

        return clean;
    }

    private static LoanSummaryDto ToSummary(Loan loan, string clientName, DateTime today) =>
        new(
            loan.Id,
            loan.ClientId,
            clientName,
            loan.PrincipalAmount,
            loan.InterestRatePerQuincena,
            loan.Quincenas,
            loan.InterestAmount,
            loan.TotalAmount,
            loan.LoanDate,
            loan.DueDate,
            loan.TotalPaid,
            loan.RemainingBalance,
            loan.Status.ToString(),
            loan.IsOverdue(today),
            loan.Notes);

    private static LoanDetailDto ToDetail(Loan loan, string clientName, DateTime today)
    {
        var payments = loan.Payments
            .OrderBy(p => p.PaymentDate)
            .Select(p => new PaymentDto(
                p.Id,
                p.PaymentDate,
                p.Amount,
                p.Notes,
                p.Allocations.Sum(a => a.PrincipalPaid),
                p.Allocations.Sum(a => a.InterestPaid)))
            .ToList();

        return new LoanDetailDto(ToSummary(loan, clientName, today), payments);
    }
}
