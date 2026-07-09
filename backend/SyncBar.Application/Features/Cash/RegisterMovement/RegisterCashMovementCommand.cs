using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.RegisterMovement;

public sealed record RegisterCashMovementCommand(
    long CashSessionId,
    long CashMovementTypeId,
    long EmployeeId,
    decimal Amount,
    string? Description) : ICommand<long>;
