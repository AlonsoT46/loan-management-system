namespace ControlDeDinero.Application.Auth;

public interface IAuthService
{
    /// <summary>Returns a token when the email and password are right; null otherwise.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
}
