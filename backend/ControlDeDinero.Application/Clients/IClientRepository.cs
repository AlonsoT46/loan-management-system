using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Clients;

/// <summary>
/// What the use cases need from the data layer. Application defines the contract;
/// Infrastructure implements it with EF Core.
/// </summary>
public interface IClientRepository
{
    Task AddAsync(Client client, CancellationToken cancellationToken = default);

    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> SearchAsync(string? term, bool includeInactive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> FindActiveByNameAsync(string name, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
