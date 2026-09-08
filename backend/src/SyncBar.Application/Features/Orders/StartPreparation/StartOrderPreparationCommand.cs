using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.StartPreparation;

public sealed record StartOrderPreparationCommand(long CustomerOrderId) : ICommand;
