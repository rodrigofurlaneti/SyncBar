using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record CreateIFoodInterruptionCommand(
    long BranchId, string Description, DateTime Start, DateTime End) : ICommand;
