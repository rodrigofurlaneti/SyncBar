using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Keeta.Order.GetById;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.GetByDisplayId
{
    public sealed record GetKeetaOrderByDisplayIdQuery(
        string DisplayId) : IQuery<KeetaIntegrationOrderResponse>;
}
