using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

public sealed record AcknowledgeIfoodOperationalAlertCommand(long CompanyId, Guid AlertId) : ICommand;
