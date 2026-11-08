using ControlDeDinero.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ControlDeDinero.Infrastructure.Identity;

public record UserProvisioningResult(bool Succeeded, IReadOnlyList<string> Errors, int AdoptedClients);

/// <summary>
/// Creates accounts. There is deliberately no public "register" endpoint: accounts are created
/// from the command line (see CreateUserCommand in the Api project).
/// </summary>
public class UserProvisioner
{
    private readonly UserManager<ApplicationUser> _users;
    private readonly AppDbContext _db;

    public UserProvisioner(UserManager<ApplicationUser> users, AppDbContext db)
    {
        _users = users;
        _db = db;
    }

    public async Task<UserProvisioningResult> CreateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = email,
            Email = email
        };

        var result = await _users.CreateAsync(user, password);
        if (!result.Succeeded)
            return new UserProvisioningResult(false, result.Errors.Select(e => e.Description).ToList(), 0);

        // Clients created before accounts existed have no owner: the first account adopts them.
        var orphans = await _db.Clients.Where(c => c.OwnerId == null).ToListAsync(cancellationToken);
        foreach (var client in orphans)
            client.OwnerId = user.Id;

        await _db.SaveChangesAsync(cancellationToken);

        return new UserProvisioningResult(true, Array.Empty<string>(), orphans.Count);
    }
}
