namespace SyncBar.Application.Features.Auth.CustomerLogin
{
    public sealed record CustomerLoginResponse(
        string AccessToken,
        DateTime ExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt,
        string UserName,
        long CustomerId,
        long CompanyId
    );
}
