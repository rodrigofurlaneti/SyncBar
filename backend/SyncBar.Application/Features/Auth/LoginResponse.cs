namespace SyncBar.Application.Features.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    string UserName);
