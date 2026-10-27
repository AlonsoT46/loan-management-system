using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Loans;

public interface ILoanRepository
{
    Task AddAsync(Loan loan, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the loan tracked, with its client, payments and each payment's allocations.
    /// Registering a payment needs the previous allocations to split the new one correctly.
    /// </summary>
    Task<Loan?> GetByIdWithPaymentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <param name="overdueAsOf">When set, only active loans with a balance whose due date is before that day.</param>
    Task<IReadOnlyList<Loan>> SearchAsync(
        Guid? clientId,
        LoanStatus? status,
        DateTime? overdueAsOf,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
