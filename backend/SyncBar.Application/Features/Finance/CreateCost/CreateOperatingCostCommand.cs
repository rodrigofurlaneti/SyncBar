using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.CreateCost;

public sealed record CreateOperatingCostCommand(
    long BranchId,
    long CostTypeId,
    string Description,
    decimal Amount,
    int ReferenceYear,
    int ReferenceMonth) : ICommand<long>;
