namespace SyncBar.Application.Features.Integrations.Keeta.Setting.GetById
{
    public sealed record KeetaIntegrationSettingResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string? ClientId,
        string? AppId,
        string BaseUrl,
        bool HasAccessToken,
        DateTime? TokenExpiresAtUtc,
        DateTime UpdatedAtUtc);
}
