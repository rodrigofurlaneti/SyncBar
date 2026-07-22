using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.GetById;

public sealed record GetOrderByIdQuery(long CustomerOrderId) : IQuery<OrderResponse>;
