using ControlDeDinero.Application.Loans;
using ControlDeDinero.Domain;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api.Controllers;

[ApiController]
[Route("api/loans")]
public class LoansController : ControllerBase
{
    private readonly LoanService _service;

    public LoansController(LoanService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<LoanDetailDto>> Create(CreateLoanRequest request, CancellationToken cancellationToken)
    {
        var loan = await _service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = loan.Loan.Id }, loan);
    }

    /// <summary>
    /// Lists loans. Examples: /api/loans?onlyOverdue=true shows who is late;
    /// /api/loans?month=2026-11 shows the loans made in November 2026.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LoanSummaryDto>>> Search(
        [FromQuery] Guid? clientId,
        [FromQuery] LoanStatus? status,
        [FromQuery] bool onlyOverdue,
        [FromQuery] string? month,
        CancellationToken cancellationToken)
    {
        var loans = await _service.SearchAsync(clientId, status, onlyOverdue, month, cancellationToken);
        return Ok(loans);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<LoanDetailDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _service.GetAsync(id, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<LoanDetailDto>> RegisterPayment(
        Guid id,
        RegisterPaymentRequest request,
        CancellationToken cancellationToken)
    {
        var loan = await _service.RegisterPaymentAsync(id, request, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    /// <summary>Voids a payment made by mistake. It is not deleted: it stays flagged in the history.</summary>
    [HttpPost("{id:guid}/payments/{paymentId:guid}/void")]
    public async Task<ActionResult<LoanDetailDto>> VoidPayment(
        Guid id,
        Guid paymentId,
        [FromBody] VoidPaymentRequest? request,
        CancellationToken cancellationToken)
    {
        var loan = await _service.VoidPaymentAsync(id, paymentId, request, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    /// <summary>Cancels a loan that has no active payments (for example, one registered by mistake).</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<LoanDetailDto>> Cancel(
        Guid id,
        [FromBody] CancelLoanRequest? request,
        CancellationToken cancellationToken)
    {
        var loan = await _service.CancelAsync(id, request, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    /// <summary>Archives a loan the lender gives up for lost. It stops counting as money to collect.</summary>
    [HttpPost("{id:guid}/archive")]
    public async Task<ActionResult<LoanDetailDto>> Archive(
        Guid id,
        [FromBody] ArchiveLoanRequest? request,
        CancellationToken cancellationToken)
    {
        var loan = await _service.ArchiveAsync(id, request, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    /// <summary>Brings an archived loan back to active (or paid, if nothing is left).</summary>
    [HttpPost("{id:guid}/unarchive")]
    public async Task<ActionResult<LoanDetailDto>> Unarchive(Guid id, CancellationToken cancellationToken)
    {
        var loan = await _service.UnarchiveAsync(id, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }

    /// <summary>
    /// Corrects a loan. Client, principal and dates only while it has no payments (otherwise 400);
    /// notes always.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<LoanDetailDto>> Update(
        Guid id,
        UpdateLoanRequest request,
        CancellationToken cancellationToken)
    {
        var loan = await _service.UpdateAsync(id, request, cancellationToken);
        if (loan is null)
            return NotFound();

        return Ok(loan);
    }
}
