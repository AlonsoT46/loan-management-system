using System;
using System.Collections.Generic;
using System.Text;

namespace ControlDelDinero.Domain
{
    public class Client
    {
        public Guid Id { get; set; }
        

        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Notes { get; set; }

        public DateTime CreatedAtUtc { get; set; } //DateTime.UtcNow
        = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;


        public ICollection<Loan> Loans { get; set; } = new List<Loan>(); //list all loans of the client
    }   
}
