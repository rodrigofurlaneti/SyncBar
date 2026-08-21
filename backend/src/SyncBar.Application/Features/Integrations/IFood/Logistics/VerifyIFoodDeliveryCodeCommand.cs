using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

// Verifica o código de entrega informado pelo cliente ao entregador — passo final do fluxo de
// logística própria. Retorna bool (CodeMatched): true = código correto, entrega concluída;
// false = código digitado errado, a chamada em si funcionou mas a verificação falhou (o
// entregador pode tentar de novo — o iFood não documenta limite de tentativas).
public sealed record VerifyIFoodDeliveryCodeCommand(long IFoodOrderId, string Code) : ICommand<bool>;
