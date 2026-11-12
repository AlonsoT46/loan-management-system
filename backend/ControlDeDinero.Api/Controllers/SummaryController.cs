using ControlDeDinero.Application.Summary;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api.Controllers;

[ApiController]
[Route("api/summary")]
public class SummaryController : ControllerBase
{
    private readonly SummaryService _service;

    public SummaryController(SummaryService service)
    {
        _service = service;
    }

    /// <summary>
    /// Financial overview: lent, recovered, interest collected, outstanding and overdue.
    /// Add ?month=2026-11 to limit "lent" and "collected" to that month.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<SummaryDto>> Get([FromQuery] string? month, CancellationToken cancellationToken)
    {
        var summary = await _service.GetAsync(month, cancellationToken);
        return Ok(summary);
    }

    /// <summary>
    /// Month by month for the charts: /api/summary/monthly?months=6 gives the last 6 months,
    /// oldest first (lent, still out, principal and interest collected).
    /// </summary>
    [HttpGet("monthly")]
    public async Task<ActionResult<IReadOnlyList<MonthlyPointDto>>> Monthly([FromQuery] int? months, CancellationToken cancellationToken)
    {
        var points = await _service.GetMonthlyAsync(months ?? 6, cancellationToken);
        return Ok(points);
    }
}
