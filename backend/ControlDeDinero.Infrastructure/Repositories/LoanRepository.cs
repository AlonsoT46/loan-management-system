using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;
using ControlDeDinero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlDeDinero.Infrastructure.Repositories;

public class LoanRepository : ILoanRepository
{
    private readonly AppDbContext _db;

    public LoanRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        await _db.Loans.AddAsync(loan, cancellationToken);
    }

    // Tracked on purpose: RegisterPayment changes the loan and adds a payment, and
    // SaveChanges persists both in one transaction.
    public Task<Loan?> GetByIdWithPaymentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Loans
            .Include(l => l.Client)
            .Include(l => l.Payments)
                .ThenInclude(p => p.Allocations)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Loan>> SearchAsync(
        Guid? clientId,
        LoanStatus? status,
        DateTime? overdueAsOf,
        DateTime? loanDateFrom,
        DateTime? loanDateTo,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Loan> query = _db.Loans
            .AsNoTracking()
            .Include(l => l.Client);

        if (clientId.HasValue)
            query = query.Where(l => l.ClientId == clientId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (overdueAsOf.HasValue)
        {
            var today = overdueAsOf.Value.Date;
            query = query.Where(l =>
                l.Status == LoanStatus.Active &&
                l.RemainingBalance > 0m &&
                l.DueDate < today);
        }

        if (loanDateFrom.HasValue)
            query = query.Where(l => l.LoanDate >= loanDateFrom.Value);

        if (loanDateTo.HasValue)
            query = query.Where(l => l.LoanDate < loanDateTo.Value);

        return await query.OrderBy(l => l.DueDate).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Loan>> ListAllWithPaymentsAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Loans
            .AsNoTracking()
            .Include(l => l.Payments)
                .ThenInclude(p => p.Allocations)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> SearchPaymentsAsync(
        Guid? clientId,
        DateTime? from,
        DateTime? to,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Payment> query = _db.Payments
            .AsNoTracking()
            .Include(p => p.Allocations)
            .Include(p => p.Loan)
                .ThenInclude(l => l!.Client);

        if (clientId.HasValue)
            query = query.Where(p => p.Loan!.ClientId == clientId.Value);

        if (!includeVoided)
            query = query.Where(p => !p.IsVoided);

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate < to.Value);

        return await query.OrderByDescending(p => p.PaymentDate).ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
