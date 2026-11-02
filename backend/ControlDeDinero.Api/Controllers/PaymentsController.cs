using ControlDeDinero.Application.Loans;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api.Controllers;

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly LoanService _service;

    public PaymentsController(LoanService service)
    {
        _service = service;
    }

    /// <summary>
    /// Payment history across all loans, newest first. Example: /api/payments?month=2026-11.
    /// Voided payments are hidden unless includeVoided=true.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PaymentListItemDto>>> Search(
        [FromQuery] Guid? clientId,
        [FromQuery] string? month,
        [FromQuery] bool includeVoided,
        CancellationToken cancellationToken)
    {
        var payments = await _service.SearchPaymentsAsync(clientId, month, includeVoided, cancellationToken);
        return Ok(payments);
    }
}
