using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9c — disputas Handshake (POST disputes/{disputeId}/alternatives/{alternativeId}). Mesma
// ressalva de AcceptIFoodDisputeCommand: sem ingestão local de eventos de disputa, a equipe
// informa o DisputeId/AlternativeId recebidos no app/painel do iFood. Amount/Currency são
// opcionais — só se aplicam a alternativas que envolvem valor (ex.: desconto), nulas quando a
// alternativa não pede valor (ex.: reagendamento).
public sealed record RequestIFoodDisputeAlternativeCommand(
    long BranchId, string DisputeId, string AlternativeId, string AlternativeType, decimal? Amount, string? Currency)
    : ICommand<IFoodDisputeActionResponse>;
