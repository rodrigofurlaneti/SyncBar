using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — atualiza o preço de vários produtos em lote (assíncrono — a resposta traz uma URL
// pra consultar o resultado do lote via GetBatchResultAsync) (POST catalog/v2.0/merchants/{merchantId}/products/price).
// Espelha 1:1 o IfoodBatchProductPriceItem do client.
public sealed record IfoodBatchProductPriceInput(
    string? ProductId, string? ExternalCode, decimal Value, decimal? OriginalValue, IReadOnlyCollection<string>? Resources);

public sealed record IfoodBatchDispatchResponse(string? Url, string? BatchId);

public sealed record BatchUpdateIfoodProductPricesCommand(
    long BranchId, IReadOnlyCollection<IfoodBatchProductPriceInput> Items, string? CatalogContext)
    : ICommand<IfoodBatchDispatchResponse>;
