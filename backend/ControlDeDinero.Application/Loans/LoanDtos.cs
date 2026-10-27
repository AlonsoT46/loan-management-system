namespace ControlDeDinero.Application.Loans;

public record PaymentDto(
    Guid Id,
    DateTime PaymentDate,
    decimal Amount,
    string? Notes,
    decimal PrincipalPaid,
    decimal InterestPaid);

/// <summary>A loan without its payments, used for lists.</summary>
public record LoanSummaryDto(
    Guid Id,
    Guid ClientId,
    string ClientName,
    decimal PrincipalAmount,
    decimal InterestRatePerQuincena,
    int Quincenas,
    decimal InterestAmount,
    decimal TotalAmount,
    DateTime LoanDate,
    DateTime DueDate,
    decimal TotalPaid,
    decimal RemainingBalance,
    string Status,
    bool IsOverdue,
    string? Notes);

/// <summary>A loan with its payments, ordered by date.</summary>
public record LoanDetailDto(LoanSummaryDto Loan, IReadOnlyList<PaymentDto> Payments);
