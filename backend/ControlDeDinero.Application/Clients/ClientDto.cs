using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Clients;

/// <summary>What the API returns about a client. The domain entity is never exposed directly.</summary>
public record ClientDto(
    Guid Id,
    string Name,
    string? Phone,
    string? Notes,
    bool IsActive,
    DateTime CreatedAtUtc)
{
    public static ClientDto From(Client client) =>
        new(client.Id, client.Name, client.Phone, client.Notes, client.IsActive, client.CreatedAtUtc);
}
