using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetById
{
    public sealed record GetKeetaOrderByIdQuery(
        long Id) : IQuery<KeetaIntegrationOrderResponse>;
}
