using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Products;

// Fase 10 — edita um produto (PUT catalog/v2.0/merchants/{merchantId}/products/{productId}).
public sealed record EditIFoodProductCommand(
    long BranchId, Guid ProductId, string Name, string? Description, string? AdditionalInformation,
    string? ExternalCode, string? Ean, string? Image, IReadOnlyCollection<IFoodProductShiftInput>? Shifts)
    : ICommand<IFoodProductResponse>;
