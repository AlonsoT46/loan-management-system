namespace ControlDeDinero.Application.Auth;

public interface IAuthService
{
    /// <summary>Returns a token when the email and password are right; null otherwise.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the password of a logged-in user after checking the current one. Every other session
    /// stops working; the returned token replaces the one used for this request.
    /// Throws ValidationException when the current password is wrong or the new one is too weak.
    /// </summary>
    Task<LoginResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
}
