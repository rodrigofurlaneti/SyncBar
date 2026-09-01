using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — cria um produto (POST catalog/v2.0/merchants/{merchantId}/products).
// IfoodProductShiftInput espelha 1:1 o IfoodProductShift do client — compartilhado por
// Create/Edit, os dois handlers que montam um IfoodUpsertProductRequest.
public sealed record IfoodProductShiftInput(
    string StartTime, string EndTime, bool Monday, bool Tuesday, bool Wednesday, bool Thursday,
    bool Friday, bool Saturday, bool Sunday);

public sealed record CreateIfoodProductCommand(
    long BranchId, string? Id, string Name, string? Description, string? AdditionalInformation,
    string? ExternalCode, string? Ean, string? Image, IReadOnlyCollection<IfoodProductShiftInput>? Shifts)
    : ICommand<IfoodProductResponse>;
