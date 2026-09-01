using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

// ⚠️ RISCO CONHECIDO: operação DESTRUTIVA e IRREVERSÍVEL contra o catálogo real do merchant no
// Ifood (POST catalog/v2.0/merchants/{merchantId}/upgrade) — migra a estrutura viva do catálogo
// de v1 pra v2 (categorias, produtos, itens e opções são reorganizados pelo Ifood do lado deles;
// não há operação simétrica de "desfazer a migração", só o downgrade explícito, que também é
// destrutivo — ver DowngradeIfoodCatalogVersionCommand). Nunca disparar automaticamente: só deve
// ser acionado a partir de uma confirmação explícita do usuário na UI, com o merchant e a filial
// afetados claramente visíveis na tela de confirmação.
public sealed record UpgradeIfoodCatalogVersionCommand(long BranchId, bool? CleanMigration) : ICommand;
