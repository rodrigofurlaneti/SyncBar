using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.DeactivateCost;

public sealed record DeactivateOperatingCostCommand(long OperatingCostId) : ICommand;
