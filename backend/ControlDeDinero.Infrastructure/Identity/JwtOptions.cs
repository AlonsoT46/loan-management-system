namespace ControlDeDinero.Infrastructure.Identity;

/// <summary>
/// Settings for the login token. Issuer, Audience and ExpiresMinutes can live in appsettings;
/// Key is a secret and must come from user-secrets (Jwt:Key) or an environment variable.
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ControlDeDinero";
    public string Audience { get; set; } = "ControlDeDinero.Clients";

    /// <summary>How long a token works. Default: 7 days, so nobody logs in every day on the phone.</summary>
    public int ExpiresMinutes { get; set; } = 60 * 24 * 7;

    /// <summary>Secret used to sign tokens. At least 32 characters.</summary>
    public string Key { get; set; } = string.Empty;
}
