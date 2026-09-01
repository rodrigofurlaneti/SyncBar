using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

public sealed record GetIfoodReconciliationOnDemandStatusQuery(long BranchId, string RequestId)
    : IQuery<IfoodReconciliationOnDemandStatusResponse>;
