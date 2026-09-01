using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

// Fase 10 — lista os produtos que casam com um código externo
// (GET catalog/v2.0/merchants/{merchantId}/products/externalCode/{externalCode}).
public sealed record ListIfoodProductsByExternalCodeQuery(long BranchId, string ExternalCode)
    : IQuery<IReadOnlyCollection<IfoodProductResponse>>;
