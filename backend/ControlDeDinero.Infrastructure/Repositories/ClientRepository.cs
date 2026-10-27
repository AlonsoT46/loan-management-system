using ControlDeDinero.Application.Clients;
using ControlDeDinero.Domain;
using ControlDeDinero.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ControlDeDinero.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _db;

    public ClientRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _db.Clients.AddAsync(client, cancellationToken);
    }

    public Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.Clients.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    // Text comparisons are case-insensitive because MySQL's default collation is.
    public async Task<IReadOnlyList<Client>> SearchAsync(
        string? term,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Clients.AsNoTracking();

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrEmpty(term))
            query = query.Where(c => c.Name.Contains(term));

        return await query.OrderBy(c => c.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Client>> FindActiveByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return await _db.Clients
            .AsNoTracking()
            .Where(c => c.IsActive && c.Name == name)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _db.SaveChangesAsync(cancellationToken);
    }
}
