using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

public sealed record AcknowledgeIFoodOperationalAlertCommand(long CompanyId, Guid AlertId) : ICommand;
