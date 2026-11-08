using ControlDeDinero.Application.Auth;
using Microsoft.AspNetCore.Identity;

namespace ControlDeDinero.Infrastructure.Identity;

/// <summary>Checks the email and password with Identity and, if they are right, issues a token.</summary>
public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly JwtTokenGenerator _tokens;

    public AuthService(UserManager<ApplicationUser> users, JwtTokenGenerator tokens)
    {
        _users = users;
        _tokens = tokens;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim();
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(request.Password))
            return null;

        var user = await _users.FindByEmailAsync(email);
        if (user is null)
            return null;

        // After several wrong passwords Identity locks the account for a few minutes.
        if (await _users.IsLockedOutAsync(user))
            return null;

        if (!await _users.CheckPasswordAsync(user, request.Password))
        {
            await _users.AccessFailedAsync(user);
            return null;
        }

        await _users.ResetAccessFailedCountAsync(user);
        return _tokens.Create(user);
    }
}
