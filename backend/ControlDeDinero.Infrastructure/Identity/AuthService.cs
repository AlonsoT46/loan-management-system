using ControlDeDinero.Application;
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

    public async Task<LoginResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            throw new ValidationException("Current and new password are required.");

        var user = await _users.FindByIdAsync(userId.ToString())
            ?? throw new ValidationException("User not found.");

        // Identity checks the current password, validates the new one and changes the SecurityStamp.
        var result = await _users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new ValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));

        // The token used for this request now has an old stamp, so hand out a fresh one.
        return _tokens.Create(user);
    }
}
