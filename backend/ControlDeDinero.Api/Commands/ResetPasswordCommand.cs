using ControlDeDinero.Infrastructure.Identity;

namespace ControlDeDinero.Api.Commands;

/// <summary>
/// Sets a new password for an account that forgot it:
///   dotnet run --project ControlDeDinero.Api -- reset-password someone@example.com
/// It also unlocks the account and closes every open session (phones must log in again).
/// </summary>
public static class ResetPasswordCommand
{
    public const string Name = "reset-password";

    public static async Task<int> RunAsync(WebApplication app, string[] args)
    {
        if (args.Length != 2)
        {
            Console.Error.WriteLine("Usage: dotnet run --project ControlDeDinero.Api -- reset-password <email>");
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
        var errors = await provisioner.ResetPasswordAsync(email, password);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
                Console.Error.WriteLine("- " + error);
            return 1;
        }

        Console.WriteLine($"Password changed for {email}. The account is unlocked and its open sessions no longer work.");
        return 0;
    }
}
