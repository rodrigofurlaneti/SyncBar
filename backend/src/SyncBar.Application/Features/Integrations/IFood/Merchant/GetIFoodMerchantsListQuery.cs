using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Fase 9c — List merchants (GET merchant/v1.0/merchants). Por CompanyId (não BranchId/MerchantId)
// porque não é específico de uma loja — lista todas as lojas habilitadas pro client_id da empresa,
// útil pra conferir se o MerchantId mapeado numa filial (IFoodMerchantMapping) realmente existe do
// lado do iFood.
public sealed record IFoodMerchantSummaryResponse(string Id, string? Name, string? CorporateName);

public sealed record GetIFoodMerchantsListQuery(long CompanyId, int Page = 1, int Size = 100)
    : IQuery<IReadOnlyCollection<IFoodMerchantSummaryResponse>>;
