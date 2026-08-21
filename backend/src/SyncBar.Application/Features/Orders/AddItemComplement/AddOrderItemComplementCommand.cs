using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.AddItemComplement;

// Adiciona um complemento a um item JÁ lançado no pedido (ex.: cliente pediu bacon extra depois
// que o hambúrguer já estava na conta) — diferente da seleção de complementos feita junto com
// AddOrderItemCommand no momento do lançamento inicial.
public sealed record AddOrderItemComplementCommand(
    long CustomerOrderId,
    long OrderItemId,
    long ComplementGroupId,
    long ComplementId,
    long? EmployeeId) : ICommand;
