namespace ControlDelDinero.Domain
{
    public class Loan
    {
        public Guid Id { get; set; }
        public Guid ClientId { get; set; }

        public decimal PrincipalAmount { get; set; }
        public decimal InterestRate { get; set; } = 15m;
        public decimal InterestAmount { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime LoanDate { get; set; }
        public DateTime? DueDate { get; set; }

        public decimal TotalPaid { get; set; }
        public decimal RemainingBalance { get; set; }

        public string Status { get; set; } = "Pending";
        public string? Notes { get; set; }

        public Client? Client { get; set; }
        public ICollection<Installment> Installments { get; set; } = new List<Installment>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<AuditEvent> AuditEvents { get; set; } = new List<AuditEvent>();
    }
}

