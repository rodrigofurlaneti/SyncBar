using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record GetIFoodInterruptionsQuery(long BranchId) : IQuery<IReadOnlyCollection<IFoodInterruptionResponse>>;
