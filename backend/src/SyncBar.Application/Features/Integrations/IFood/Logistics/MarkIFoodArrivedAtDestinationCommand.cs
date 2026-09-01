using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Entregador chegou no endereço do cliente (destino).
public sealed record MarkIfoodArrivedAtDestinationCommand(long IfoodOrderId) : ICommand;
