using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Cash.CloseSession;

// Conferencia manual do operador para uma forma de pagamento nao-dinheiro
// (Cartao de Credito/Debito/Pix). O valor esperado NAO vem daqui — o handler
// sempre recalcula a partir das vendas liquidadas da sessao, para nao confiar
// em um "esperado" que o cliente poderia manipular.
public sealed record PaymentMethodCountRequest(long PaymentMethodId, decimal CountedAmount);

public sealed record CloseCashSessionCommand(
    long CashSessionId,
    long ClosedByEmployeeId,
    decimal ClosingAmount,
    IReadOnlyCollection<PaymentMethodCountRequest>? PaymentMethodCounts = null) : ICommand<CloseCashSessionResponse>;
