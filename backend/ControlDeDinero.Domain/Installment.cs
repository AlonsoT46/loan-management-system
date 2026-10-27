namespace ControlDeDinero.Domain;

public class Installment
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }

    public int Number { get; set; }
    public DateTime DueDate { get; set; }
    public decimal ScheduledAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public InstallmentStatus Status { get; set; } = InstallmentStatus.Pending;

    public Loan? Loan { get; set; }
}
