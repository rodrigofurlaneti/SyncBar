using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Fase 9c — List merchants (GET merchant/v1.0/merchants). Por CompanyId (não BranchId/MerchantId)
// porque não é específico de uma loja — lista todas as lojas habilitadas pro client_id da empresa,
// útil pra conferir se o MerchantId mapeado numa filial (IfoodMerchantMapping) realmente existe do
// lado do Ifood.
public sealed record IfoodMerchantSummaryResponse(string Id, string? Name, string? CorporateName);

public sealed record GetIfoodMerchantsListQuery(long CompanyId, int Page = 1, int Size = 100)
    : IQuery<IReadOnlyCollection<IfoodMerchantSummaryResponse>>;
