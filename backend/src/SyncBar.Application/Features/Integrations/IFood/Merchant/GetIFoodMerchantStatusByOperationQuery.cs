using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Fase 9c — Get merchant status by operation (GET merchant/v1.0/merchants/{id}/status/{operation}).
// Diferente de GetIFoodMerchantStatusQuery (status geral, primeira operação da lista): aqui a
// tela escolhe qual operação consultar (ex.: "DELIVERY", "TAKEOUT") — os valores válidos vêm do
// próprio catálogo de operações do iFood, não temos uma lista fixa confirmada nesta fase.
public sealed record GetIFoodMerchantStatusByOperationQuery(long BranchId, string Operation)
    : IQuery<IFoodMerchantStatusByOperationResponse>;

public sealed record IFoodMerchantStatusByOperationResponse(
    string? Operation, string? SalesChannel, bool Available, string? State,
    IReadOnlyCollection<IFoodMerchantValidationResponse> Validations);
