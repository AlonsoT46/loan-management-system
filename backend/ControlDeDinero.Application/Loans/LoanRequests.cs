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
