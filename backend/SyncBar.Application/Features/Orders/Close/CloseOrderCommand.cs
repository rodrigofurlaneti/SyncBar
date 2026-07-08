using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.Close;

// ServiceFeeRate padrao de 10% — taxa de servico brasileira.
public sealed record CloseOrderCommand(long CustomerOrderId, decimal ServiceFeeRate = 0.10m) : ICommand;
