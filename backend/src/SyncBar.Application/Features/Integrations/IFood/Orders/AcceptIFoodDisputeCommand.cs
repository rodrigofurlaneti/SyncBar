using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

// Fase 9b — disputas Handshake (POST disputes/{disputeId}/accept). Sem ingestão local de eventos
// de disputa ainda: a equipe informa o DisputeId recebido no app/painel do iFood. Por BranchId
// (não por IFoodOrderId local) porque a disputa não está necessariamente ligada a um IFoodOrder
// já sincronizado neste banco.
public sealed record IFoodDisputeActionResponse(bool Success, string? Status);

public sealed record AcceptIFoodDisputeCommand(long BranchId, string DisputeId) : ICommand<IFoodDisputeActionResponse>;
