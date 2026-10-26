using System;
using System.Collections.Generic;
using System.Text;

namespace ControlDelDinero.Domain
{
    public class PaymentAllocation
    {
        public Guid Id { get; set; }
        public Guid PaymentId { get; set; }
        public Guid LoanId { get; set; }

        public decimal PrincipalPaid { get; set; }
        public decimal InterestPaid { get; set; }

        public Payment? Payment { get; set; }
        public Loan? Loan { get; set; }
    }
}
