namespace ControlDeDinero.Api.Commands;

/// <summary>Terminal input shared by the account commands (create-user, reset-password).</summary>
internal static class ConsoleInput
{
    /// <summary>Asks for a new password twice, hidden. Returns null (and says why) if they differ.</summary>
    public static string? ReadNewPassword()
    {
        var password = ReadPassword("New password: ");
        var confirmation = ReadPassword("Repeat the password: ");

        if (password != confirmation)
        {
            Console.Error.WriteLine("The passwords do not match. Nothing was changed.");
            return null;
        }

        return password;
    }

    /// <summary>Reads a line without showing it on screen, so it never stays in the terminal.</summary>
    public static string ReadPassword(string prompt)
    {
        Console.Write(prompt);
        var chars = new List<char>();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (key.Key == ConsoleKey.Backspace)
            {
                if (chars.Count > 0)
                    chars.RemoveAt(chars.Count - 1);
                continue;
            }

            if (!char.IsControl(key.KeyChar))
                chars.Add(key.KeyChar);
        }

        return new string(chars.ToArray());
    }
}
