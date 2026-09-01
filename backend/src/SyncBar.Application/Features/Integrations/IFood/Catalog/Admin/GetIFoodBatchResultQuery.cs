using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

// Fase 10 — consulta o resultado de um processamento em lote assíncrono
// (GET catalog/v2.0/merchants/{merchantId}/batch/{batchId}) — usado, por exemplo, após um
// BatchUpdateProductPrices (que devolve um BatchId a acompanhar).
public sealed record IfoodBatchResultItemResponse(string? ResourceId, string? Result, string? FailureReason);

public sealed record IfoodBatchStatusResponse(string? BatchStatus, IReadOnlyCollection<IfoodBatchResultItemResponse> Results);

public sealed record GetIfoodBatchResultQuery(long BranchId, string BatchId) : IQuery<IfoodBatchStatusResponse>;
