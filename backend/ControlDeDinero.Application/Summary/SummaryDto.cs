namespace ControlDeDinero.Application.Summary;

/// <summary>
/// Financial overview. With a month, "lent" and "collected" cover only that month (loans by loan
/// date, payments by payment date); the balance and overdue figures are always as of today.
/// </summary>
/// <param name="Month">The requested "yyyy-MM", or null for all time.</param>
/// <param name="TotalLent">Principal of loans made in the period (cancelled loans excluded).</param>
/// <param name="ExpectedInterest">Interest agreed on the loans made in the period: the expected profit (net).</param>
/// <param name="ExpectedTotal">TotalLent + ExpectedInterest: everything those loans should bring back (gross).</param>
/// <param name="PrincipalRecovered">Principal returned by payments in the period (voided payments excluded).</param>
/// <param name="InterestCollected">Interest earned from payments in the period (voided payments excluded).</param>
/// <param name="TotalCollected">PrincipalRecovered + InterestCollected.</param>
/// <param name="OutstandingBalance">What active loans still owe, as of today.</param>
/// <param name="ActiveLoans">Number of active loans.</param>
/// <param name="OverdueLoans">Active loans past their due date, as of today.</param>
/// <param name="OverdueBalance">What the overdue loans still owe.</param>
/// <param name="DueThisMonth">What active loans due from today to the end of this month still owe (overdue ones not included).</param>
/// <param name="DueThisMonthLoans">How many loans that is.</param>
/// <param name="ArchivedLoans">Loans the lender archived (wrote off).</param>
/// <param name="ArchivedBalance">What the archived loans still owed when archived: money given up for lost.</param>
public record SummaryDto(
    string? Month,
    decimal TotalLent,
    decimal ExpectedInterest,
    decimal ExpectedTotal,
    decimal PrincipalRecovered,
    decimal InterestCollected,
    decimal TotalCollected,
    decimal OutstandingBalance,
    int ActiveLoans,
    int OverdueLoans,
    decimal OverdueBalance,
    decimal DueThisMonth,
    int DueThisMonthLoans,
    int ArchivedLoans,
    decimal ArchivedBalance);
