using ControlDeDinero.Application;
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
        try
        {
            var loan = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = loan.Loan.Id }, loan);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Lists loans. Example: /api/loans?onlyOverdue=true shows who is late.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<LoanSummaryDto>>> Search(
        [FromQuery] Guid? clientId,
        [FromQuery] LoanStatus? status,
        [FromQuery] bool onlyOverdue,
        CancellationToken cancellationToken)
    {
        var loans = await _service.SearchAsync(clientId, status, onlyOverdue, cancellationToken);
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
        try
        {
            var loan = await _service.RegisterPaymentAsync(id, request, cancellationToken);
            if (loan is null)
                return NotFound();

            return Ok(loan);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
}
