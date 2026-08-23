using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.AddPizzaItem;

// Fase 17 — lança uma pizza no pedido. Diferente de AddOrderItemCommand: o preço não vem do
// Product (Product.SalePrice é ignorado pra produtos com PizzaConfiguration, ver comentário na
// entidade) — é calculado aqui a partir do tamanho/borda/recheio/sabores escolhidos
// (PizzaConfiguration.CalculateUnitPrice). Sempre 1 OrderItem por pizza, mesmo fracionada.
public sealed record AddPizzaOrderItemCommand(
    long CustomerOrderId,
    long ProductId,
    decimal Quantity,
    string? Notes,
    long? EmployeeId,
    long PizzaSizeId,
    long? PizzaCrustId,
    long? PizzaEdgeId,
    IReadOnlyCollection<long> PizzaFlavorIds) : ICommand;
