using Microsoft.AspNetCore.Identity;

namespace ControlDeDinero.Infrastructure.Identity;

/// <summary>
/// Decides whether a correctly signed token still counts. Each token carries the user's
/// SecurityStamp from the moment it was issued; Identity replaces the stamp whenever the password
/// changes or is reset. If the stamps differ, the token belongs to an old session and is rejected.
/// Cost: one small query to the users table per request.
/// </summary>
public class TokenSessionValidator
{
    private readonly UserManager<ApplicationUser> _users;

    public TokenSessionValidator(UserManager<ApplicationUser> users)
    {
        _users = users;
    }

    public async Task<bool> IsStillValidAsync(string? userId, string? stamp)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(stamp))
            return false;

        var user = await _users.FindByIdAsync(userId);
        return user is not null && user.SecurityStamp == stamp;
    }
}
