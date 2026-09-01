using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood;

public sealed record SetIfoodMerchantMappingCommand(long BranchId, string? MerchantId, string? MerchantUuid) : ICommand;
