using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record CreateIfoodInterruptionCommand(
    long BranchId, string Description, DateTime Start, DateTime End) : ICommand;
