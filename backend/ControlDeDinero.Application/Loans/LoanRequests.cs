namespace ControlDeDinero.Application.Loans;

/// <param name="LoanDate">Defaults to today (Costa Rica) when omitted.</param>
/// <param name="DueDate">Required. The number of quincenas is derived from it.</param>
/// <param name="InterestRatePerQuincena">Percent per quincena; defaults to 20 when omitted.</param>
public record CreateLoanRequest(
    Guid ClientId,
    decimal PrincipalAmount,
    DateTime? LoanDate,
    DateTime? DueDate,
    decimal? InterestRatePerQuincena,
    string? Notes);

/// <param name="PaymentDate">Defaults to today (Costa Rica) when omitted.</param>
public record RegisterPaymentRequest(decimal Amount, DateTime? PaymentDate, string? Notes);

public record VoidPaymentRequest(string? Reason);

public record CancelLoanRequest(string? Reason);

public record ArchiveLoanRequest(string? Reason);

/// <summary>
/// Correction of a loan. Client, principal and dates can only change while the loan has no payments;
/// the notes can always change. LoanDate defaults to the current one when omitted.
/// </summary>
public record UpdateLoanRequest(
    Guid ClientId,
    decimal PrincipalAmount,
    DateTime? LoanDate,
    DateTime? DueDate,
    string? Notes);
