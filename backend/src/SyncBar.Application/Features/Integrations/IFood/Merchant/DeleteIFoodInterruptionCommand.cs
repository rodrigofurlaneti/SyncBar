using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record DeleteIFoodInterruptionCommand(long BranchId, string InterruptionId) : ICommand;
