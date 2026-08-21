using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood;

public sealed record SetIFoodMerchantMappingCommand(long BranchId, string? MerchantId, string? MerchantUuid) : ICommand;
