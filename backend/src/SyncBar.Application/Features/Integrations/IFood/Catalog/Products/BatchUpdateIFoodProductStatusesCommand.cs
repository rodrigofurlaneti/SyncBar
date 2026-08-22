using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

// Fase 10 — atualiza o status (disponibilidade) de vários produtos em lote
// (PATCH catalog/v2.0/merchants/{merchantId}/products/status). Espelha 1:1 o
// IFoodBatchProductStatusItem do client.
public sealed record IFoodBatchProductStatusInput(
    string? ProductId, string? ExternalCode, string Status, IReadOnlyCollection<string>? Resources);

public sealed record BatchUpdateIFoodProductStatusesCommand(
    long BranchId, IReadOnlyCollection<IFoodBatchProductStatusInput> Items, string? CatalogContext) : ICommand;
