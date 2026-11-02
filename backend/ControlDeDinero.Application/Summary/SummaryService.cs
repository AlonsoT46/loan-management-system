using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Summary;

public class SummaryService
{
    private readonly ILoanRepository _loans;
    private readonly TimeProvider _time;

    public SummaryService(ILoanRepository loans, TimeProvider time)
    {
        _loans = loans;
        _time = time;
    }

    /// <param name="month">Optional "yyyy-MM". Empty means all time.</param>
    public async Task<SummaryDto> GetAsync(string? month, CancellationToken cancellationToken = default)
    {
        var (from, to) = MonthRange.Parse(month);
        var today = _time.GetLocalNow().Date;

        // For a personal ledger it is simple and fast enough to compute in memory.
        var loans = (await _loans.ListAllWithPaymentsAsync(cancellationToken))
            .Where(l => l.Status != LoanStatus.Cancelled)
            .ToList();

        var totalLent = loans
            .Where(l => MonthRange.Contains(l.LoanDate, from, to))
            .Sum(l => l.PrincipalAmount);

        var payments = loans
            .SelectMany(l => l.Payments)
            .Where(p => !p.IsVoided && MonthRange.Contains(p.PaymentDate, from, to))
            .ToList();

        var principalRecovered = payments.Sum(p => p.Allocations.Sum(a => a.PrincipalPaid));
        var interestCollected = payments.Sum(p => p.Allocations.Sum(a => a.InterestPaid));

        var active = loans.Where(l => l.Status == LoanStatus.Active).ToList();
        var overdue = active.Where(l => l.IsOverdue(today)).ToList();

        return new SummaryDto(
            string.IsNullOrWhiteSpace(month) ? null : month.Trim(),
            totalLent,
            principalRecovered,
            interestCollected,
            principalRecovered + interestCollected,
            active.Sum(l => l.RemainingBalance),
            active.Count,
            overdue.Count,
            overdue.Sum(l => l.RemainingBalance));
    }
}
