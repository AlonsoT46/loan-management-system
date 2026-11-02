using ControlDeDinero.Application;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api;

/// <summary>
/// Turns a ValidationException thrown anywhere in the use cases into an HTTP 400 with a
/// ProblemDetails body. Any other exception is left to the default handler (HTTP 500).
/// </summary>
public sealed class ValidationExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validation)
            return false;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation error",
                Detail = validation.Message
            },
            cancellationToken);

        return true;
    }
}
