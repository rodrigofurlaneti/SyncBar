using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Fase 9c — Get merchant status by operation (GET merchant/v1.0/merchants/{id}/status/{operation}).
// Diferente de GetIfoodMerchantStatusQuery (status geral, primeira operação da lista): aqui a
// tela escolhe qual operação consultar (ex.: "DELIVERY", "TAKEOUT") — os valores válidos vêm do
// próprio catálogo de operações do Ifood, não temos uma lista fixa confirmada nesta fase.
public sealed record GetIfoodMerchantStatusByOperationQuery(long BranchId, string Operation)
    : IQuery<IfoodMerchantStatusByOperationResponse>;

public sealed record IfoodMerchantStatusByOperationResponse(
    string? Operation, string? SalesChannel, bool Available, string? State,
    IReadOnlyCollection<IfoodMerchantValidationResponse> Validations);
