using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

public sealed record StartIFoodOrderPreparationCommand(long IFoodOrderId) : ICommand;
