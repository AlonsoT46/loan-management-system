namespace ControlDeDinero.Application.Auth;

public record LoginRequest(string? Email, string? Password);

/// <param name="Token">Sent back on every later request as: Authorization: Bearer (token).</param>
public record LoginResponse(string Token, DateTime ExpiresAtUtc);

public record MeResponse(Guid UserId);
