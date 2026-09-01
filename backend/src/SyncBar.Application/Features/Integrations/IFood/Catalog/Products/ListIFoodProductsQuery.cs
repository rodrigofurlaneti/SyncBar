using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — lista os produtos do merchant (GET catalog/v2.0/merchants/{merchantId}/products).
// IfoodProductResponse é compartilhado por todos os handlers do módulo Products que
// devolvem/recebem um produto (List/Create/Edit/ListByExternalCode/GetById).
public sealed record IfoodProductResponse(
    string? Id, string? Name, string? Description, string? AdditionalInformation, string? ExternalCode,
    string? Ean, bool? Industrialized, string? ImagePath);

public sealed record ListIfoodProductsQuery(long BranchId, int? Limit = null, int? Page = null)
    : IQuery<IReadOnlyCollection<IfoodProductResponse>>;
