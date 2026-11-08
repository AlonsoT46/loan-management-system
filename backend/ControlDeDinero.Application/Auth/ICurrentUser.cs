namespace ControlDeDinero.Application.Auth;

/// <summary>
/// Who is making the current request. Application only declares the promise; the Api layer
/// fulfils it by reading the logged-in user from the token (like a repository, but for identity).
/// </summary>
public interface ICurrentUser
{
    Guid UserId { get; }
}
