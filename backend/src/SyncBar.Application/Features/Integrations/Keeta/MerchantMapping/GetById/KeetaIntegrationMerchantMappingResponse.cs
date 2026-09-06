namespace SyncBar.Application.Features.Integrations.Keeta.MerchantMapping.GetById
{
    public sealed record KeetaIntegrationMerchantMappingResponse(
        long Id,
        long CompanyId,
        long BranchId,
        string InternalMerchantId,
        long KeetaMerchantId,
        string StoreName,
        string? TimeZone,
        bool IsAuthorized,
        bool IsOnboarded,
        DateTime? LastMenuSyncAtUtc,
        string? MenuBaseUrl,
        string? WebhookUrl,
        DateTime CreatedAtUtc);
}
