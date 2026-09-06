using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetActiveOrdersByBranch
{
    public sealed record GetActiveKeetaOrdersByBranchQuery(
        long BranchId) : IQuery<IReadOnlyList<KeetaIntegrationOrderResponse>>;
}
