using ControlDeDinero.Application.Clients;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Tests;

// In-memory stand-ins for the database and the clock, so use cases are tested without MySQL.

internal sealed class FakeClientRepository : IClientRepository
{
    public List<Client> Clients { get; } = new();
    public int SaveCount { get; private set; }

    public Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        Clients.Add(client);
        return Task.CompletedTask;
    }

    public Task<Client?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.OwnerId == ownerId && c.Id == id));

    public Task<IReadOnlyList<Client>> SearchAsync(Guid ownerId, string? term, bool includeInactive, CancellationToken cancellationToken = default)
    {
        IEnumerable<Client> query = Clients.Where(c => c.OwnerId == ownerId);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(term))
            query = query.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<Client> result = query.OrderBy(c => c.Name).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Client>> FindActiveByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Client> result = Clients
            .Where(c => c.OwnerId == ownerId && c.IsActive && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(result);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeLoanRepository : ILoanRepository
{
    private readonly FakeClientRepository _clients;

    public FakeLoanRepository(FakeClientRepository clients)
    {
        _clients = clients;
    }

    public List<Loan> Loans { get; } = new();
    public int SaveCount { get; private set; }

    public Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        Loans.Add(loan);
        return Task.CompletedTask;
    }

    // A loan belongs to whoever owns its client, as in the real database.
    private IEnumerable<Loan> OwnedBy(Guid ownerId) =>
        Loans.Where(l => _clients.Clients.Any(c => c.Id == l.ClientId && c.OwnerId == ownerId));

    public Task<Loan?> GetByIdWithPaymentsAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(OwnedBy(ownerId).FirstOrDefault(l => l.Id == id));

    public Task<IReadOnlyList<Loan>> SearchAsync(
        Guid ownerId,
        Guid? clientId,
        LoanStatus? status,
        DateTime? overdueAsOf,
        DateTime? loanDateFrom,
        DateTime? loanDateTo,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Loan> query = OwnedBy(ownerId);

        if (clientId.HasValue)
            query = query.Where(l => l.ClientId == clientId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (overdueAsOf.HasValue)
        {
            var today = overdueAsOf.Value.Date;
            query = query.Where(l => l.Status == LoanStatus.Active && l.RemainingBalance > 0m && l.DueDate < today);
        }

        if (loanDateFrom.HasValue)
            query = query.Where(l => l.LoanDate >= loanDateFrom.Value);

        if (loanDateTo.HasValue)
            query = query.Where(l => l.LoanDate < loanDateTo.Value);

        IReadOnlyList<Loan> result = query.OrderBy(l => l.DueDate).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Loan>> ListAllWithPaymentsAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Loan> result = OwnedBy(ownerId).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Payment>> SearchPaymentsAsync(
        Guid ownerId,
        Guid? clientId,
        DateTime? from,
        DateTime? to,
        bool includeVoided,
        CancellationToken cancellationToken = default)
    {
        var payments = new List<Payment>();

        foreach (var loan in OwnedBy(ownerId))
        {
            if (clientId.HasValue && loan.ClientId != clientId.Value)
                continue;

            foreach (var payment in loan.Payments)
            {
                // EF fills Payment.Loan when it loads; the fake does the same.
                SetProperty(payment, nameof(Payment.Loan), loan);
                payments.Add(payment);
            }
        }

        IEnumerable<Payment> query = payments;

        if (!includeVoided)
            query = query.Where(p => !p.IsVoided);

        if (from.HasValue)
            query = query.Where(p => p.PaymentDate >= from.Value);

        if (to.HasValue)
            query = query.Where(p => p.PaymentDate < to.Value);

        IReadOnlyList<Payment> result = query.OrderByDescending(p => p.PaymentDate).ToList();
        return Task.FromResult(result);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // EF generates the keys of new payments when saving; the fake does the same.
        foreach (var payment in Loans.SelectMany(l => l.Payments).Where(p => p.Id == Guid.Empty))
            SetProperty(payment, nameof(Payment.Id), Guid.NewGuid());

        SaveCount++;
        return Task.CompletedTask;
    }

    private static void SetProperty(object target, string name, object? value) =>
        target.GetType().GetProperty(name)!.SetValue(target, value);
}

/// <summary>The logged-in user of the test. Change UserId to act as somebody else.</summary>
internal sealed class FakeCurrentUser : ControlDeDinero.Application.Auth.ICurrentUser
{
    public static readonly Guid DefaultUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid UserId { get; set; } = DefaultUserId;
}

/// <summary>A clock frozen at a given UTC instant, whose local time zone is UTC-6 (Costa Rica).</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private static readonly TimeZoneInfo CostaRica =
        TimeZoneInfo.CreateCustomTimeZone("CR", TimeSpan.FromHours(-6), "Costa Rica", "Costa Rica");

    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow)
    {
        _utcNow = utcNow;
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override TimeZoneInfo LocalTimeZone => CostaRica;
}
