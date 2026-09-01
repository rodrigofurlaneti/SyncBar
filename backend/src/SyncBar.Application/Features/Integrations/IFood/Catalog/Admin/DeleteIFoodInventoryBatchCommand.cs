using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

// Fase 10 — remove o estoque de vários produtos em lote (DELETE catalog/v2.0/merchants/{merchantId}/inventory/batch).
public sealed record DeleteIfoodInventoryBatchCommand(long BranchId, IReadOnlyCollection<Guid> ProductIds) : ICommand;
