namespace ControlDeDinero.Application.Summary;

/// <summary>
/// Financial overview. With a month, "lent" and "collected" cover only that month (loans by loan
/// date, payments by payment date); the balance and overdue figures are always as of today.
/// </summary>
/// <param name="Month">The requested "yyyy-MM", or null for all time.</param>
/// <param name="TotalLent">Principal of loans made in the period (cancelled loans excluded).</param>
/// <param name="PrincipalRecovered">Principal returned by payments in the period (voided payments excluded).</param>
/// <param name="InterestCollected">Interest earned from payments in the period (voided payments excluded).</param>
/// <param name="TotalCollected">PrincipalRecovered + InterestCollected.</param>
/// <param name="OutstandingBalance">What active loans still owe, as of today.</param>
/// <param name="ActiveLoans">Number of active loans.</param>
/// <param name="OverdueLoans">Active loans past their due date, as of today.</param>
/// <param name="OverdueBalance">What the overdue loans still owe.</param>
public record SummaryDto(
    string? Month,
    decimal TotalLent,
    decimal PrincipalRecovered,
    decimal InterestCollected,
    decimal TotalCollected,
    decimal OutstandingBalance,
    int ActiveLoans,
    int OverdueLoans,
    decimal OverdueBalance);
