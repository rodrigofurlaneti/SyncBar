using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

// Fase 10 — consulta o resultado de um processamento em lote assíncrono
// (GET catalog/v2.0/merchants/{merchantId}/batch/{batchId}) — usado, por exemplo, após um
// BatchUpdateProductPrices (que devolve um BatchId a acompanhar).
public sealed record IFoodBatchResultItemResponse(string? ResourceId, string? Result, string? FailureReason);

public sealed record IFoodBatchStatusResponse(string? BatchStatus, IReadOnlyCollection<IFoodBatchResultItemResponse> Results);

public sealed record GetIFoodBatchResultQuery(long BranchId, string BatchId) : IQuery<IFoodBatchStatusResponse>;
