using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Purchases.GetByBranch;

public sealed record GetPurchasesByBranchQuery(long BranchId) : IQuery<IReadOnlyCollection<PurchaseResponse>>;
