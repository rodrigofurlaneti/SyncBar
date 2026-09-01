using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record GetIfoodInterruptionsQuery(long BranchId) : IQuery<IReadOnlyCollection<IfoodInterruptionResponse>>;
