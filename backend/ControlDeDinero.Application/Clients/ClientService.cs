using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Clients;

public class ClientService
{
    public const int MaxNameLength = 150;
    public const int MaxPhoneLength = 30;
    public const int MaxNotesLength = 500;

    private readonly IClientRepository _repository;

    public ClientService(IClientRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateClientResult> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        var (name, phone, notes) = Validate(request.Name, request.Phone, request.Notes);

        // Looked up before adding, so the new client is not reported as its own duplicate.
        var duplicates = await _repository.FindActiveByNameAsync(name, cancellationToken);

        var client = new Client
        {
            Id = Guid.NewGuid(),
            Name = name,
            Phone = phone,
            Notes = notes
        };

        await _repository.AddAsync(client, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new CreateClientResult(
            ClientDto.From(client),
            duplicates.Select(ClientDto.From).ToList());
    }

    public async Task<ClientDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _repository.GetByIdAsync(id, cancellationToken);
        return client is null ? null : ClientDto.From(client);
    }

    public async Task<IReadOnlyList<ClientDto>> SearchAsync(
        string? term,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var cleanTerm = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
        var clients = await _repository.SearchAsync(cleanTerm, includeInactive, cancellationToken);
        return clients.Select(ClientDto.From).ToList();
    }

    public async Task<ClientDto?> UpdateAsync(Guid id, UpdateClientRequest request, CancellationToken cancellationToken = default)
    {
        var (name, phone, notes) = Validate(request.Name, request.Phone, request.Notes);

        var client = await _repository.GetByIdAsync(id, cancellationToken);
        if (client is null)
            return null;

        client.Name = name;
        client.Phone = phone;
        client.Notes = notes;

        await _repository.SaveChangesAsync(cancellationToken);
        return ClientDto.From(client);
    }

    public Task<ClientDto?> DeactivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, false, cancellationToken);

    public Task<ClientDto?> ActivateAsync(Guid id, CancellationToken cancellationToken = default) =>
        SetActiveAsync(id, true, cancellationToken);

    private async Task<ClientDto?> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var client = await _repository.GetByIdAsync(id, cancellationToken);
        if (client is null)
            return null;

        client.IsActive = isActive;

        await _repository.SaveChangesAsync(cancellationToken);
        return ClientDto.From(client);
    }

    private static (string Name, string? Phone, string? Notes) Validate(string? name, string? phone, string? notes)
    {
        var cleanName = (name ?? string.Empty).Trim();
        if (cleanName.Length == 0)
            throw new ValidationException("Name is required.");
        if (cleanName.Length > MaxNameLength)
            throw new ValidationException($"Name cannot be longer than {MaxNameLength} characters.");

        var cleanPhone = NullIfBlank(phone);
        if (cleanPhone is not null && cleanPhone.Length > MaxPhoneLength)
            throw new ValidationException($"Phone cannot be longer than {MaxPhoneLength} characters.");

        var cleanNotes = NullIfBlank(notes);
        if (cleanNotes is not null && cleanNotes.Length > MaxNotesLength)
            throw new ValidationException($"Notes cannot be longer than {MaxNotesLength} characters.");

        return (cleanName, cleanPhone, cleanNotes);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
