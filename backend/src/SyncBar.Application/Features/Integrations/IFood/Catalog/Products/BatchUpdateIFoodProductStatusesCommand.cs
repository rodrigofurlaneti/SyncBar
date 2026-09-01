using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — atualiza o status (disponibilidade) de vários produtos em lote
// (PATCH catalog/v2.0/merchants/{merchantId}/products/status). Espelha 1:1 o
// IfoodBatchProductStatusItem do client.
public sealed record IfoodBatchProductStatusInput(
    string? ProductId, string? ExternalCode, string Status, IReadOnlyCollection<string>? Resources);

public sealed record BatchUpdateIfoodProductStatusesCommand(
    long BranchId, IReadOnlyCollection<IfoodBatchProductStatusInput> Items, string? CatalogContext) : ICommand;
