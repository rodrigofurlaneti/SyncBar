namespace SyncBar.Application.Features.Orders.AddItem;

// Uma escolha de complemento feita no momento do lançamento do item — ex.: ComplementGroupId do
// grupo "Escolha uma bebida", ComplementId da opção "Coca-Cola" dentro dele. O preço (ExtraPrice)
// não vem do cliente: é sempre resolvido no handler a partir do Complement cadastrado, pra não
// permitir manipulação de preço pelo request.
public sealed record OrderItemComplementSelection(long ComplementGroupId, long ComplementId);
