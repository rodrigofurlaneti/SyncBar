using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

public sealed record GetIFoodReconciliationOnDemandStatusQuery(long BranchId, string RequestId)
    : IQuery<IFoodReconciliationOnDemandStatusResponse>;
