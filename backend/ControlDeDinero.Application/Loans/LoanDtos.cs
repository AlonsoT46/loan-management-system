namespace ControlDeDinero.Application.Loans;

public record PaymentDto(
    Guid Id,
    DateTime PaymentDate,
    decimal Amount,
    string? Notes,
    decimal PrincipalPaid,
    decimal InterestPaid,
    bool IsVoided,
    DateTime? VoidedAtUtc,
    string? VoidReason);

/// <summary>A payment in a cross-loan list (history by month or by client).</summary>
public record PaymentListItemDto(
    Guid PaymentId,
    Guid LoanId,
    Guid ClientId,
    string ClientName,
    DateTime PaymentDate,
    decimal Amount,
    decimal PrincipalPaid,
    decimal InterestPaid,
    bool IsVoided);

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

/// <summary>A loan with its payments, ordered by date (voided ones included and flagged).</summary>
public record LoanDetailDto(LoanSummaryDto Loan, IReadOnlyList<PaymentDto> Payments);
