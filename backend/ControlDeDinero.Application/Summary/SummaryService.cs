using System.Globalization;
using ControlDeDinero.Application.Auth;
using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;

namespace ControlDeDinero.Application.Summary;

public class SummaryService
{
    private readonly ILoanRepository _loans;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public SummaryService(ILoanRepository loans, ICurrentUser currentUser, TimeProvider time)
    {
        _loans = loans;
        _currentUser = currentUser;
        _time = time;
    }

    /// <param name="month">Optional "yyyy-MM". Empty means all time.</param>
    public async Task<SummaryDto> GetAsync(string? month, CancellationToken cancellationToken = default)
    {
        var (from, to) = MonthRange.Parse(month);
        var today = _time.GetLocalNow().Date;

        // For a personal ledger it is simple and fast enough to compute in memory.
        var loans = (await _loans.ListAllWithPaymentsAsync(_currentUser.UserId, cancellationToken))
            .Where(l => l.Status != LoanStatus.Cancelled)
            .ToList();

        var lentInPeriod = loans.Where(l => MonthRange.Contains(l.LoanDate, from, to)).ToList();
        var totalLent = lentInPeriod.Sum(l => l.PrincipalAmount);
        var expectedInterest = lentInPeriod.Sum(l => l.InterestAmount);

        var payments = loans
            .SelectMany(l => l.Payments)
            .Where(p => !p.IsVoided && MonthRange.Contains(p.PaymentDate, from, to))
            .ToList();

        var principalRecovered = payments.Sum(p => p.Allocations.Sum(a => a.PrincipalPaid));
        var interestCollected = payments.Sum(p => p.Allocations.Sum(a => a.InterestPaid));

        var active = loans.Where(l => l.Status == LoanStatus.Active).ToList();
        var overdue = active.Where(l => l.IsOverdue(today)).ToList();

        // "To collect this month": from today to the end of the current calendar month, overdue ones apart.
        var monthEnd = new DateTime(today.Year, today.Month, 1).AddMonths(1);
        var dueThisMonth = active.Where(l => !l.IsOverdue(today) && l.DueDate.Date < monthEnd).ToList();

        var archived = loans.Where(l => l.Status == LoanStatus.Archived).ToList();

        return new SummaryDto(
            string.IsNullOrWhiteSpace(month) ? null : month.Trim(),
            totalLent,
            expectedInterest,
            totalLent + expectedInterest,
            principalRecovered,
            interestCollected,
            principalRecovered + interestCollected,
            active.Sum(l => l.RemainingBalance),
            active.Count,
            overdue.Count,
            overdue.Sum(l => l.RemainingBalance),
            dueThisMonth.Sum(l => l.RemainingBalance),
            dueThisMonth.Count,
            archived.Count,
            archived.Sum(l => l.RemainingBalance));
    }

    public const int MaxMonths = 24;

    /// <summary>
    /// The last <paramref name="months"/> months, oldest first, ending with the current month.
    /// Each month: what was lent (and how much of it is still out) and what came in (principal and interest).
    /// </summary>
    public async Task<IReadOnlyList<MonthlyPointDto>> GetMonthlyAsync(int months, CancellationToken cancellationToken = default)
    {
        if (months < 1 || months > MaxMonths)
            throw new ValidationException($"Months must be between 1 and {MaxMonths}.");

        var today = _time.GetLocalNow().Date;

        var loans = (await _loans.ListAllWithPaymentsAsync(_currentUser.UserId, cancellationToken))
            .Where(l => l.Status != LoanStatus.Cancelled)
            .ToList();

        var payments = loans
            .SelectMany(l => l.Payments)
            .Where(p => !p.IsVoided)
            .ToList();

        var firstMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));
        var points = new List<MonthlyPointDto>(months);

        for (var i = 0; i < months; i++)
        {
            var from = firstMonth.AddMonths(i);
            var to = from.AddMonths(1);

            var lentThatMonth = loans.Where(l => MonthRange.Contains(l.LoanDate, from, to)).ToList();
            var stillOut = lentThatMonth.Where(l => l.Status == LoanStatus.Active).Sum(PrincipalLeft);
            var writtenOff = lentThatMonth.Where(l => l.Status == LoanStatus.Archived).Sum(PrincipalLeft);

            var paidThatMonth = payments.Where(p => MonthRange.Contains(p.PaymentDate, from, to)).ToList();

            points.Add(new MonthlyPointDto(
                from.ToString("yyyy-MM", CultureInfo.InvariantCulture),
                lentThatMonth.Sum(l => l.PrincipalAmount),
                stillOut,
                writtenOff,
                paidThatMonth.Sum(p => p.Allocations.Sum(a => a.PrincipalPaid)),
                paidThatMonth.Sum(p => p.Allocations.Sum(a => a.InterestPaid))));
        }

        return points;
    }

    /// <summary>Principal of a loan that has not come back (voided payments do not count).</summary>
    private static decimal PrincipalLeft(Loan loan) =>
        loan.PrincipalAmount
        - loan.Payments.Where(p => !p.IsVoided).SelectMany(p => p.Allocations).Sum(a => a.PrincipalPaid);
}
