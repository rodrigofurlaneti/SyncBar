using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;

namespace SyncBar.Application.Features.Orders.Open;

public sealed record OpenOrderCommand(
    long BranchId,
    long? DiningTableId,
    long? ComandaId,
    long EmployeeId,
    int? GuestCount,
    string? Notes,
    long OrderTypeId = OrderTypeIds.Mesa,
    string? CustomerName = null,
    string? CustomerPhone = null,
    string? DeliveryAddress = null) : ICommand<long>;
