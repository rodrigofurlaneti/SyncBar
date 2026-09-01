using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

public sealed record StartIfoodOrderPreparationCommand(long IfoodOrderId) : ICommand;
