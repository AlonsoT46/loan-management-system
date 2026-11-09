using System.Security.Claims;
using System.Text;
using ControlDeDinero.Application.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ControlDeDinero.Infrastructure.Identity;

/// <summary>Builds and signs the token a user receives after logging in.</summary>
public class JwtTokenGenerator
{
    private readonly JwtOptions _options;
    private readonly TimeProvider _time;

    public JwtTokenGenerator(IOptions<JwtOptions> options, TimeProvider time)
    {
        _options = options.Value;
        _time = time;
    }

    public LoginResponse Create(ApplicationUser user)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_options.ExpiresMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Key));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = expires,
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email ?? string.Empty),
                // Changes when the password changes; TokenSessionValidator compares it on every request.
                new Claim("stamp", user.SecurityStamp ?? string.Empty),
                new Claim("jti", Guid.NewGuid().ToString())
            }),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var token = new JsonWebTokenHandler().CreateToken(descriptor);
        return new LoginResponse(token, expires);
    }
}
