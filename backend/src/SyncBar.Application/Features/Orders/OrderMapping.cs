using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;

namespace SyncBar.Application.Features.Orders
{
    internal static class OrderMapping
    {
        internal static OrderResponse ToResponse(this CustomerOrder order, decimal partialPaidAmount = 0)
            => new(
                order.Id, order.BranchId, order.DiningTableId, order.ComandaId, order.EmployeeId,
                order.OrderStatusId, order.GuestCount, order.OpenedAt, order.ClosedAt,
                order.SubtotalAmount, order.DiscountAmount, order.ServiceFeeAmount, order.TotalAmount,
                partialPaidAmount,
                order.CreditLimitAmount,
                order.Notes,
                order.OrderTypeId,
                order.OrderOriginId,
                OrderOriginIds.GetName(order.OrderOriginId),
                order.CustomerName,
                order.CustomerPhone,
                order.DeliveryAddress,
                order.Items
                    .Where(i => i.IsActive)
                    .Select(i => new OrderItemResponse(
                        i.Id,
                        i.ProductId,
                        i.OrderItemStatusId,
                        i.Quantity,
                        i.UnitPrice,
                        i.DiscountAmount,
                        i.TotalAmount,
                        i.Notes,
                        i.EmployeeId,
                        i.Complements
                            .Where(c => c.IsActive)
                            .Select(c => new OrderItemComplementResponse(c.Id, c.ComplementId, c.UnitPriceCharged))
                            .ToList())
                    {
                        OptionalExtras = i.OptionalExtras.Where(x => x.IsActive).Select(x => new OrderItemOptionalExtraResponse(x.ProductOptionalExtraId, x.Name)).ToArray(),
                        Boosts = i.Boosts.Where(x => x.IsActive).Select(x => new OrderItemBoostResponse(x.ProductBoostId, x.Name, x.UnitPriceCharged)).ToArray()
                    })
                    .ToList());
    }
}
