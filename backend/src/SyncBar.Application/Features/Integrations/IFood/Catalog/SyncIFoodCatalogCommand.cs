using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog;

// Sincroniza o cardápio inteiro (categorias + produtos ativos) da empresa com o catálogo do
// Ifood, em toda filial com merchant mapeado e integração habilitada. Disparado automaticamente
// (fire-and-forget, ver IIfoodCatalogSyncTrigger) sempre que um produto/categoria é
// criado/editado/desativado, e também manualmente pelo botão "Sincronizar agora" na tela de
// integrações — mesmo comando nos dois casos, sempre um resync completo (essential flow: sem
// sincronização incremental/diff, o volume de um cardápio de bar/restaurante não justifica).
public sealed record SyncIfoodCatalogCommand(long CompanyId) : ICommand<IfoodCatalogSyncSummary>;
