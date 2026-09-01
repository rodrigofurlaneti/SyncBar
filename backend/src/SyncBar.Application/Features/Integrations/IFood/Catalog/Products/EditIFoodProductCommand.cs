using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — edita um produto (PUT catalog/v2.0/merchants/{merchantId}/products/{productId}).
public sealed record EditIfoodProductCommand(
    long BranchId, Guid ProductId, string Name, string? Description, string? AdditionalInformation,
    string? ExternalCode, string? Ean, string? Image, IReadOnlyCollection<IfoodProductShiftInput>? Shifts)
    : ICommand<IfoodProductResponse>;
