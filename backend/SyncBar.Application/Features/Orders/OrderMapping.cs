using SyncBar.Domain.Entities;

namespace SyncBar.Application.Features.Orders;

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
            order.CustomerName,
            order.CustomerPhone,
            order.DeliveryAddress,
            order.Items
                .Where(i => i.IsActive)
                .Select(i => new OrderItemResponse(
                    i.Id, i.ProductId, i.OrderItemStatusId, i.Quantity, i.UnitPrice,
                    i.DiscountAmount, i.TotalAmount, i.Notes))
                .ToList());
}
