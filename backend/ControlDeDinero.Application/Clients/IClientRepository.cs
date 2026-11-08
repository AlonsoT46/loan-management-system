using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Clients;

/// <summary>
/// What the use cases need from the data layer. Application defines the contract;
/// Infrastructure implements it with EF Core. Every query takes the owner's id: a client that
/// belongs to another user is never returned.
/// </summary>
public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid ownerId, Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> SearchAsync(Guid ownerId, string? term, bool includeInactive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> FindActiveByNameAsync(Guid ownerId, string name, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
