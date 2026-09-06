using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByKeetaOrderId
{
    public sealed record GetKeetaOrderByKeetaOrderIdQuery(
        string KeetaOrderId) : IQuery<KeetaIntegrationOrderResponse>;
}
