namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed record IfoodMerchantMappingResponse(long BranchId, string BranchName, string? MerchantId, string? MerchantUuid);
