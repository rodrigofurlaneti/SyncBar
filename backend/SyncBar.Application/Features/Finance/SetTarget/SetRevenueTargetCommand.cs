using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.SetTarget;

public sealed record SetRevenueTargetCommand(
    long BranchId,
    int ReferenceYear,
    int ReferenceMonth,
    decimal TargetAmount) : ICommand<long>;
