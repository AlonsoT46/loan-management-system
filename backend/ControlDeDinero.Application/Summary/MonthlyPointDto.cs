namespace ControlDeDinero.Application.Summary;

/// <summary>One month of the charts (cancelled loans and voided payments excluded).</summary>
/// <param name="Month">"yyyy-MM".</param>
/// <param name="Lent">Principal of the loans made that month.</param>
/// <param name="PrincipalStillOut">Of that principal, what has not come back yet on active loans (as of today).</param>
/// <param name="PrincipalWrittenOff">Of that principal, what did not come back on loans archived as lost.</param>
/// <param name="PrincipalCollected">Principal returned by payments made that month (any loan).</param>
/// <param name="InterestCollected">Interest earned by payments made that month (any loan).</param>
public record MonthlyPointDto(
    string Month,
    decimal Lent,
    decimal PrincipalStillOut,
    decimal PrincipalWrittenOff,
    decimal PrincipalCollected,
    decimal InterestCollected);
