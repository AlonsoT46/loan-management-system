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
}
