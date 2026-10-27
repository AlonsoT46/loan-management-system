namespace ControlDeDinero.Domain;

public class AuditEvent
{
    private AuditEvent() { } // EF Core

    internal AuditEvent(Guid loanId, string eventType, string description)
    {
        // Id is left empty on purpose: EF generates it when saving. A new child that already
        // has an Id and is added through a tracked parent would be treated as an existing row.
        LoanId = loanId;
        EventType = eventType;
        Description = description;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }

    public string EventType { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    public Loan? Loan { get; private set; }
}
