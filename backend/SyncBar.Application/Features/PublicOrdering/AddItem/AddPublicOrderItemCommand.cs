using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.PublicOrdering.AddItem;

// Sem autenticação — o "segredo" é o token do QR Code da mesa (GUID imprevisível).
// Abre o pedido da mesa automaticamente na primeira chamada (dono = Branch.SelfServiceEmployeeId).
public sealed record AddPublicOrderItemCommand(
    Guid Token,
    long ProductId,
    decimal Quantity,
    string? Notes) : ICommand<long>;
