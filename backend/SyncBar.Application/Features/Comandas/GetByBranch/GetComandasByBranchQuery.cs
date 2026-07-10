using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Comandas.GetByBranch;

public sealed record GetComandasByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<ComandaResponse>>;
