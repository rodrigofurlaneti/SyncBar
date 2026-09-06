using SyncBar.Application.Abstractions.Messaging;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId
{
    public sealed record ExistsKeetaOrderByKeetaOrderIdQuery(
        string KeetaOrderId) : IQuery<bool>;
}
