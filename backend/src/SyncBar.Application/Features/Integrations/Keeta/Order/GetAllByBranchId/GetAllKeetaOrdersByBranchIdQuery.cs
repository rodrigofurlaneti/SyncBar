using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetAllByBranchId
{
    public sealed record GetAllKeetaOrdersByBranchIdQuery(
        long BranchId) : IQuery<IReadOnlyList<KeetaIntegrationOrderResponse>>;
}
