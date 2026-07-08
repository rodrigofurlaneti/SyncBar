using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.ApplyDiscount;

public sealed record ApplyOrderDiscountCommand(long CustomerOrderId, decimal DiscountAmount) : ICommand;
