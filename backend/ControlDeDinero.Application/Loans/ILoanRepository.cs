using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Loans;

/// <summary>
/// Every query takes the owner's id and only returns loans whose client belongs to that user.
/// </summary>
public interface ILoanRepository
{
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the loan tracked, with its client, payments and each payment's allocations.
    /// Registering or voiding a payment needs the previous allocations to recalculate correctly.
    /// </summary>
    Task<Loan?> GetByIdWithPaymentsAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default);

    /// <param name="overdueAsOf">When set, only active loans with a balance whose due date is before that day.</param>
    /// <param name="loanDateFrom">When set, only loans dated on or after this day.</param>
    /// <param name="loanDateTo">When set, only loans dated before this day (exclusive).</param>
    Task<IReadOnlyList<Loan>> SearchAsync(
        Guid ownerId,
        Guid? clientId,
        LoanStatus? status,
        DateTime? overdueAsOf,
        DateTime? loanDateFrom,
        DateTime? loanDateTo,
        CancellationToken cancellationToken = default);

    /// <summary>Every loan of the owner with its payments and allocations, not tracked. Used for the summary.</summary>
    Task<IReadOnlyList<Loan>> ListAllWithPaymentsAsync(Guid ownerId, CancellationToken cancellationToken = default);

    /// <summary>Payments with their allocations and loan (and its client), newest first.</summary>
    Task<IReadOnlyList<Payment>> SearchPaymentsAsync(
        Guid ownerId,
        Guid? clientId,
        DateTime? from,
        DateTime? to,
        bool includeVoided,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
