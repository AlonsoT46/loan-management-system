using ControlDeDinero.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControlDeDinero.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ICurrentUser _currentUser;

    public AuthController(IAuthService auth, ICurrentUser currentUser)
    {
        _auth = auth;
        _currentUser = currentUser;
    }

    /// <summary>The only endpoint that works without a token. Wrong credentials always give the same 401.</summary>
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await _auth.LoginAsync(request, cancellationToken);
        if (response is null)
            return Problem(title: "Invalid email or password.", statusCode: StatusCodes.Status401Unauthorized);

        return Ok(response);
    }

    /// <summary>Handy to check that a token works: returns the id of the logged-in user.</summary>
    [HttpGet("me")]
    public ActionResult<MeResponse> Me() => Ok(new MeResponse(_currentUser.UserId));
}
