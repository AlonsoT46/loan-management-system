using Microsoft.AspNetCore.Identity;

namespace ControlDeDinero.Infrastructure.Identity;

/// <summary>
/// A person who can log in. Identity gives it Email, PasswordHash (never the password itself),
/// lockout counters and more. The key is a Guid, like every other id in the project.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
}
