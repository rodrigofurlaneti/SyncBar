using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Orders.AddItem;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

// Sem autenticação — o "segredo" é o token do QR Code da mesa (GUID imprevisível).
// Abre o pedido da mesa automaticamente na primeira chamada (dono = Branch.SelfServiceEmployeeId).
// ComandaCode: quando informado, o pedido fica associado à COMANDA (billing/tab dela) em vez
// da mesa — não aparece na conta da mesa (GetPublicBill), só na da comanda (GetPublicComandaBill).
// A mesa continua registrada no Notes do pedido, pra cozinha/garçom saberem onde entregar.
public sealed record AddPublicOrderItemCommand(
    Guid Token,
    long ProductId,
    decimal Quantity,
    string? Notes,
    IReadOnlyCollection<OrderItemComplementSelection>? Complements = null,
    string? ComandaCode = null) : ICommand<long>;
