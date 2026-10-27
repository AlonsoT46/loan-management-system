namespace ControlDeDinero.Domain;

public class PaymentAllocation
{
    private PaymentAllocation() { } // EF Core

    // Id and PaymentId are set by EF when saving, because the allocation is added to Payment.Allocations.
    internal PaymentAllocation(Guid loanId, decimal principalPaid, decimal interestPaid)
    {
        LoanId = loanId;
        PrincipalPaid = principalPaid;
        InterestPaid = interestPaid;
    }

    public Guid Id { get; private set; }
    public Guid PaymentId { get; private set; }
    public Guid LoanId { get; private set; }

    public decimal PrincipalPaid { get; private set; }
    public decimal InterestPaid { get; private set; }

    public Payment? Payment { get; private set; }
    public Loan? Loan { get; private set; }
}
