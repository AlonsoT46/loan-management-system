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

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Clients.FirstOrDefault(c => c.Id == id));

    public Task<IReadOnlyList<Client>> SearchAsync(string? term, bool includeInactive, CancellationToken cancellationToken = default)
    {
        IEnumerable<Client> query = Clients;

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(term))
            query = query.Where(c => c.Name.Contains(term, StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<Client> result = query.OrderBy(c => c.Name).ToList();
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<Client>> FindActiveByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Client> result = Clients
            .Where(c => c.IsActive && string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase))
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
    public List<Loan> Loans { get; } = new();
    public int SaveCount { get; private set; }

    public Task AddAsync(Loan loan, CancellationToken cancellationToken = default)
    {
        Loans.Add(loan);
        return Task.CompletedTask;
    }

    public Task<Loan?> GetByIdWithPaymentsAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Loans.FirstOrDefault(l => l.Id == id));

    public Task<IReadOnlyList<Loan>> SearchAsync(
        Guid? clientId,
        LoanStatus? status,
        DateTime? overdueAsOf,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<Loan> query = Loans;

        if (clientId.HasValue)
            query = query.Where(l => l.ClientId == clientId.Value);

        if (status.HasValue)
            query = query.Where(l => l.Status == status.Value);

        if (overdueAsOf.HasValue)
        {
            var today = overdueAsOf.Value.Date;
            query = query.Where(l => l.Status == LoanStatus.Active && l.RemainingBalance > 0m && l.DueDate < today);
        }

        IReadOnlyList<Loan> result = query.OrderBy(l => l.DueDate).ToList();
        return Task.FromResult(result);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.CompletedTask;
    }
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
