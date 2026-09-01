using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

// Verifica o código de entrega informado pelo cliente ao entregador — passo final do fluxo de
// logística própria. Retorna bool (CodeMatched): true = código correto, entrega concluída;
// false = código digitado errado, a chamada em si funcionou mas a verificação falhou (o
// entregador pode tentar de novo — o Ifood não documenta limite de tentativas).
public sealed record VerifyIfoodDeliveryCodeCommand(long IfoodOrderId, string Code) : ICommand<bool>;
