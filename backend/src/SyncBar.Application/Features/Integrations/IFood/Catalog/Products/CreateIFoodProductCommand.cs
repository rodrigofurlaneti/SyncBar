using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

// Fase 10 — cria um produto (POST catalog/v2.0/merchants/{merchantId}/products).
// IFoodProductShiftInput espelha 1:1 o IFoodProductShift do client — compartilhado por
// Create/Edit, os dois handlers que montam um IFoodUpsertProductRequest.
public sealed record IFoodProductShiftInput(
    string StartTime, string EndTime, bool Monday, bool Tuesday, bool Wednesday, bool Thursday,
    bool Friday, bool Saturday, bool Sunday);

public sealed record CreateIFoodProductCommand(
    long BranchId, string? Id, string Name, string? Description, string? AdditionalInformation,
    string? ExternalCode, string? Ean, string? Image, IReadOnlyCollection<IFoodProductShiftInput>? Shifts)
    : ICommand<IFoodProductResponse>;
