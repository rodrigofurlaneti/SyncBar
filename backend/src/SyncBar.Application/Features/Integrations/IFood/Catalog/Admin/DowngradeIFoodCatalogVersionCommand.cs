using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

// ⚠️ RISCO CONHECIDO: assim como o upgrade (ver UpgradeIFoodCatalogVersionCommand), esta é uma
// operação DESTRUTIVA e IRREVERSÍVEL contra o catálogo real do merchant no iFood
// (POST catalog/v2.0/merchants/{merchantId}/downgrade) — reverte a estrutura viva do catálogo de
// v2 pra v1, com o mesmo risco de reorganização/perda de dados do lado do iFood. Nunca disparar
// automaticamente: só deve ser acionado a partir de uma confirmação explícita do usuário na UI,
// com o merchant e a filial afetados claramente visíveis na tela de confirmação.
public sealed record DowngradeIFoodCatalogVersionCommand(long BranchId) : ICommand;
