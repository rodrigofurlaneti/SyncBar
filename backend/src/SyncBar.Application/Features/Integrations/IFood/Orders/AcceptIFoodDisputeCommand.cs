using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

// Fase 9b — disputas Handshake (POST disputes/{disputeId}/accept). Sem ingestão local de eventos
// de disputa ainda: a equipe informa o DisputeId recebido no app/painel do Ifood. Por BranchId
// (não por IfoodOrderId local) porque a disputa não está necessariamente ligada a um IfoodOrder
// já sincronizado neste banco.
public sealed record IfoodDisputeActionResponse(bool Success, string? Status);

public sealed record AcceptIfoodDisputeCommand(long BranchId, string DisputeId) : ICommand<IfoodDisputeActionResponse>;
