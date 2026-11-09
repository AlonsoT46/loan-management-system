using ControlDeDinero.Infrastructure.Identity;

namespace ControlDeDinero.Api.Commands;

/// <summary>
/// Creates an account from the terminal:
///   dotnet run --project ControlDeDinero.Api -- create-user someone@example.com
/// The password is typed when asked (hidden), so it never stays in the shell history.
/// </summary>
public static class CreateUserCommand
{
    public const string Name = "create-user";

    public static async Task<int> RunAsync(WebApplication app, string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: dotnet run --project ControlDeDinero.Api -- create-user <email>");
            return 1;
        }

        if (Console.IsInputRedirected)
        {
            Console.Error.WriteLine("Run this command from a normal terminal: the password is typed interactively.");
            return 1;
        }

        var email = args[1].Trim();
        var password = ConsoleInput.ReadNewPassword();
        if (password is null)
            return 1;

        using var scope = app.Services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<UserProvisioner>();
        var result = await provisioner.CreateAsync(email, password);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                Console.Error.WriteLine("- " + error);
            return 1;
        }

        Console.WriteLine($"Account created: {email}");
        if (result.AdoptedClients > 0)
            Console.WriteLine($"{result.AdoptedClients} existing client(s) now belong to this account.");

        return 0;
    }
}
