using ControlDeDinero.Application;
using ControlDeDinero.Application.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ClientService _service;

    public ClientsController(ClientService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<ActionResult<CreateClientResult>> Create(CreateClientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _service.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = result.Client.Id }, result);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ClientDto>>> Search(
        [FromQuery] string? search,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken)
    {
        var clients = await _service.SearchAsync(search, includeInactive, cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var client = await _service.GetAsync(id, cancellationToken);
        if (client is null)
            return NotFound();

        return Ok(client);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ClientDto>> Update(Guid id, UpdateClientRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var client = await _service.UpdateAsync(id, request, cancellationToken);
            if (client is null)
                return NotFound();

            return Ok(client);
        }
        catch (ValidationException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("{id:guid}/deactivate")]
    public async Task<ActionResult<ClientDto>> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        var client = await _service.DeactivateAsync(id, cancellationToken);
        if (client is null)
            return NotFound();

        return Ok(client);
    }

    [HttpPost("{id:guid}/activate")]
    public async Task<ActionResult<ClientDto>> Activate(Guid id, CancellationToken cancellationToken)
    {
        var client = await _service.ActivateAsync(id, cancellationToken);
        if (client is null)
            return NotFound();

        return Ok(client);
    }
}
