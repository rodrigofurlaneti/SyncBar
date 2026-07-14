using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Orders.SplitBill;

public sealed record BillShareResponse(int PersonNumber, decimal Amount);

public sealed record BillSplitResponse(decimal TotalAmount, int PeopleCount, IReadOnlyCollection<BillShareResponse> Shares);

// Divisão igualitária da conta entre N pessoas — usa o suporte já existente a múltiplos
// pagamentos por venda (RegisterSaleCommand.Payments): cada pessoa paga o valor calculado aqui
// como um dos pagamentos da mesma venda. Não cria venda nem estado novo, só calcula os valores.
public sealed record CalculateBillSplitQuery(long CustomerOrderId, int PeopleCount) : IQuery<BillSplitResponse>;
