namespace ControlDeDinero.Application.Clients;

public record CreateClientRequest(string? Name, string? Phone, string? Notes);

public record UpdateClientRequest(string? Name, string? Phone, string? Notes);

/// <summary>
/// The created client plus any active clients that already have the same name. Two different
/// people can share a name, so duplicates are a warning, not an error.
/// </summary>
public record CreateClientResult(ClientDto Client, IReadOnlyList<ClientDto> PossibleDuplicates);
