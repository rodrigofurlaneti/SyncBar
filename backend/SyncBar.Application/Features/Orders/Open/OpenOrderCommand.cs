using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.Open;

public sealed record OpenOrderCommand(
    long BranchId,
    long? DiningTableId,
    long? ComandaId,
    long EmployeeId,
    int? GuestCount,
    string? Notes) : ICommand<long>;
