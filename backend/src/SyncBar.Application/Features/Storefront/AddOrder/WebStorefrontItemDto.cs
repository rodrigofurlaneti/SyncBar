using SyncBar.Application.Features.Orders.AddItem;
namespace SyncBar.Application.Features.Storefront.AddOrder
{
    public sealed record WebStorefrontItemDto(
        long ProductId,
        decimal Quantity,
        string? Notes,
        IReadOnlyCollection<OrderItemComplementSelection>? Complements);
}
