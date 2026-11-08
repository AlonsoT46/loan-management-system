using System.Security.Claims;
using ControlDeDinero.Application.Auth;

namespace ControlDeDinero.Api.Auth;

/// <summary>
/// Fulfils ICurrentUser by reading the user id from the token of the current HTTP request.
/// </summary>
public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid UserId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            var value = principal?.FindFirst("sub")?.Value
                ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Unreachable for normal requests: the global policy rejects them before this runs.
            if (!Guid.TryParse(value, out var id))
                throw new InvalidOperationException("There is no authenticated user in this request.");

            return id;
        }
    }
}
