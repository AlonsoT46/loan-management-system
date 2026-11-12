namespace ControlDeDinero.Domain;

public enum LoanStatus
{
    Active,
    Paid,
    Cancelled,

    /// <summary>Written off: the lender no longer expects this money back. Kept in the history.</summary>
    Archived
}
