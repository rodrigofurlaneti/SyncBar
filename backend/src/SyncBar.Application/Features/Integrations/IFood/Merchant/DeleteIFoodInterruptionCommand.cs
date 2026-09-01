using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record DeleteIfoodInterruptionCommand(long BranchId, string InterruptionId) : ICommand;
