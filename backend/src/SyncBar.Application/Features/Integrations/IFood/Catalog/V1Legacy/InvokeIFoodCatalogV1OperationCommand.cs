using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.V1Legacy;

// Fase 10 — despachante genérico pros 56 endpoints do módulo Catalog v1 (legado) que não têm
// implementação tipada dedicada. Ver comentário completo em IIFoodCatalogClient (região
// "Catálogo v1 (legado)") sobre a decisão de escopo: a v2 (viva, já usada pela sincronização
// automática desde a Fase 3) ganhou CQRS dedicado; a v1 é alcançada por este único comando
// dinâmico — o chamador escolhe a operação (enum IFoodCatalogV1Operation) e fornece os parâmetros
// de rota/query/corpo que ela precisa. Isso fecha 100% de alcance HTTP do módulo v1 sem duplicar
// um sistema de tipos inteiro pra uma API que nenhum merchant do SyncBar usa hoje.
public sealed record IFoodCatalogV1OperationResponse(bool Success, int StatusCode, string? ResponseBody, string? ErrorMessage);

public sealed record InvokeIFoodCatalogV1OperationCommand(
    long BranchId,
    IFoodCatalogV1Operation Operation,
    Dictionary<string, string>? RouteParams,
    Dictionary<string, string>? QueryParams,
    string? JsonBody) : ICommand<IFoodCatalogV1OperationResponse>;
