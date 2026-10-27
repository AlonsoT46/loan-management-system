using System;
using System.Collections.Generic;
using System.Text;

namespace ControlDelDinero.Domain
{
    public class Payment
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }

        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? Notes { get; set; }

        public Loan? Loan { get; set; }
        public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
    }
}
