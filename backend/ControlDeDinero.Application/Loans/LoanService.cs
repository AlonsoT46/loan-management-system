using ControlDeDinero.Application.Auth;
using ControlDeDinero.Application.Clients;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Loans;

public class LoanService
{
    public const int MaxNotesLength = 500;

    private readonly ILoanRepository _loans;
    private readonly IClientRepository _clients;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public LoanService(ILoanRepository loans, IClientRepository clients, ICurrentUser currentUser, TimeProvider time)
    {
        _loans = loans;
        _clients = clients;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<LoanDetailDto> CreateAsync(CreateLoanRequest request, CancellationToken cancellationToken = default)
    {
        var client = await _clients.GetByIdAsync(_currentUser.UserId, request.ClientId, cancellationToken)
            ?? throw new ValidationException("Client not found.");

        if (!client.IsActive)
            throw new ValidationException("The client is inactive.");

        if (request.DueDate is null)
            throw new ValidationException("Due date is required.");

        var notes = CleanText(request.Notes, "Notes");
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
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, id, cancellationToken);
        if (loan is null)
            return null;

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    /// <param name="month">Optional "yyyy-MM": only loans made in that month.</param>
    public async Task<IReadOnlyList<LoanSummaryDto>> SearchAsync(
        Guid? clientId,
        LoanStatus? status,
        bool onlyOverdue,
        string? month = null,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = MonthRange.Parse(month);
        var today = Today();

        var loans = await _loans.SearchAsync(
            _currentUser.UserId,
            clientId,
            status,
            onlyOverdue ? today : null,
            from,
            to,
            cancellationToken);

        return loans
            .Select(l => ToSummary(l, l.Client?.Name ?? string.Empty, today))
            .ToList();
    }

    /// <param name="month">Optional "yyyy-MM": only payments made in that month.</param>
    public async Task<IReadOnlyList<PaymentListItemDto>> SearchPaymentsAsync(
        Guid? clientId,
        string? month,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        var (from, to) = MonthRange.Parse(month);
        var payments = await _loans.SearchPaymentsAsync(_currentUser.UserId, clientId, from, to, includeVoided, cancellationToken);

        return payments
            .Select(p => new PaymentListItemDto(
                p.Id,
                p.LoanId,
                p.Loan?.ClientId ?? Guid.Empty,
                p.Loan?.Client?.Name ?? string.Empty,
                p.PaymentDate,
                p.Amount,
                p.Allocations.Sum(a => a.PrincipalPaid),
                p.Allocations.Sum(a => a.InterestPaid),
                p.IsVoided))
            .ToList();
    }

    public async Task<LoanDetailDto?> RegisterPaymentAsync(
        Guid loanId,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null)
            return null;

        var today = Today();
        var paymentDate = (request.PaymentDate ?? today).Date;
        var notes = CleanText(request.Notes, "Notes");

        try
        {
            loan.RegisterPayment(request.Amount, paymentDate, notes);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, today);
    }

    /// <summary>Voids a payment (it stays in the history, flagged). Returns null if the loan or payment does not exist.</summary>
    public async Task<LoanDetailDto?> VoidPaymentAsync(
        Guid loanId,
        Guid paymentId,
        VoidPaymentRequest? request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null || loan.Payments.All(p => p.Id != paymentId))
            return null;

        var reason = CleanText(request?.Reason, "Reason");
        var voidedAtUtc = _time.GetUtcNow().UtcDateTime;

        try
        {
            loan.VoidPayment(paymentId, voidedAtUtc, reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    /// <summary>Cancels a loan that has no active payments. Returns null if the loan does not exist.</summary>
    public async Task<LoanDetailDto?> CancelAsync(
        Guid loanId,
        CancelLoanRequest? request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null)
            return null;

        var reason = CleanText(request?.Reason, "Reason");

        try
        {
            loan.Cancel(reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);

        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    /// <summary>
    /// Corrects a loan. If client, principal or dates change, the domain recalculates the loan (only
    /// allowed without payments); if only the notes change, any loan accepts it.
    /// Returns null if the loan does not exist.
    /// </summary>
    public async Task<LoanDetailDto?> UpdateAsync(
        Guid loanId,
        UpdateLoanRequest request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null)
            return null;

        if (request.DueDate is null)
            throw new ValidationException("Due date is required.");

        var notes = CleanText(request.Notes, "Notes");
        var loanDate = (request.LoanDate ?? loan.LoanDate).Date;
        var dueDate = request.DueDate.Value.Date;
        var principal = decimal.Round(request.PrincipalAmount, 2, MidpointRounding.AwayFromZero);
        var clientName = loan.Client?.Name ?? string.Empty;

        var termsChanged =
            request.ClientId != loan.ClientId ||
            principal != loan.PrincipalAmount ||
            loanDate != loan.LoanDate.Date ||
            dueDate != loan.DueDate.Date;

        if (termsChanged)
        {
            // The new client must be one of this user's clients (another user's is "not found").
            var client = await _clients.GetByIdAsync(_currentUser.UserId, request.ClientId, cancellationToken)
                ?? throw new ValidationException("Client not found.");

            if (!client.IsActive && client.Id != loan.ClientId)
                throw new ValidationException("The client is inactive.");

            try
            {
                loan.UpdateTerms(client.Id, principal, loanDate, dueDate);
            }
            catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
            {
                throw new ValidationException(ex.Message);
            }

            clientName = client.Name;
        }

        loan.Notes = notes;

        await _loans.SaveChangesAsync(cancellationToken);
        return ToDetail(loan, clientName, Today());
    }

    /// <summary>Archives (writes off) an active loan. Returns null if the loan does not exist.</summary>
    public async Task<LoanDetailDto?> ArchiveAsync(
        Guid loanId,
        ArchiveLoanRequest? request,
        CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null)
            return null;

        var reason = CleanText(request?.Reason, "Reason");

        try
        {
            loan.Archive(reason);
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);
        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    /// <summary>Brings an archived loan back. Returns null if the loan does not exist.</summary>
    public async Task<LoanDetailDto?> UnarchiveAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        var loan = await _loans.GetByIdWithPaymentsAsync(_currentUser.UserId, loanId, cancellationToken);
        if (loan is null)
            return null;

        try
        {
            loan.Unarchive();
        }
        catch (InvalidOperationException ex)
        {
            throw new ValidationException(ex.Message);
        }

        await _loans.SaveChangesAsync(cancellationToken);
        return ToDetail(loan, loan.Client?.Name ?? string.Empty, Today());
    }

    /// <summary>Today's date in the configured time zone (Costa Rica in production).</summary>
    private DateTime Today() => _time.GetLocalNow().Date;

    private static string? CleanText(string? value, string fieldName)
    {
        var clean = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (clean is not null && clean.Length > MaxNotesLength)
            throw new ValidationException($"{fieldName} cannot be longer than {MaxNotesLength} characters.");

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
                p.Allocations.Sum(a => a.InterestPaid),
                p.IsVoided,
                p.VoidedAtUtc,
                p.VoidReason))
            .ToList();

        return new LoanDetailDto(ToSummary(loan, clientName, today), payments);
    }
}
