using System;
using System.Collections.Generic;
using System.Text;

namespace ControlDelDinero.Domain
{
    public class AuditEvent
    {
        public Guid Id { get; set; }
        public Guid LoanId { get; set; }

        public string EventType { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; private set; }
        = DateTime.UtcNow;

        public Loan? Loan { get; set; }
    }
}
