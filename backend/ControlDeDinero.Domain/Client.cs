namespace ControlDeDinero.Domain;

public class Client
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The user (account) this client belongs to. Every query is filtered by it, so a user never
    /// sees another user's clients. It is null only for clients created before accounts existed;
    /// the first account created adopts them.
    /// </summary>
    public Guid? OwnerId { get; set; }

    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}
