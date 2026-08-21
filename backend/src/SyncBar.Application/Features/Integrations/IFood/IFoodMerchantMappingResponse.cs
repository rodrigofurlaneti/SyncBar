namespace SyncBar.Application.Features.Integrations.IFood;

public sealed record IFoodMerchantMappingResponse(long BranchId, string BranchName, string? MerchantId, string? MerchantUuid);
