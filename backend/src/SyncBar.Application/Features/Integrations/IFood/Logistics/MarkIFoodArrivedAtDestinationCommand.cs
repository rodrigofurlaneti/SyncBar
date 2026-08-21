using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Entregador chegou no endereço do cliente (destino).
public sealed record MarkIFoodArrivedAtDestinationCommand(long IFoodOrderId) : ICommand;
