using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog;

// Fase 10 — passo comum a qualquer handler do módulo Catalog v2 que precise de um catalogId
// (criar/editar/listar categoria — ver comentário completo em IIfoodCatalogClient e no bug
// corrigido em SyncIfoodCatalogCommandHandler). Um merchant pode ter mais de um "catálogo"
// (contextos diferentes — ex.: Marketplace vs Cardápio Digital), mas a doc oficial não documenta
// qual escolher quando há mais de um; esta resolução pega o primeiro com status "AVAILABLE" (ou,
// na ausência de qualquer um com esse status, o primeiro item da lista) — mesma cautela já usada
// noutras fases quando a doc não é explícita sobre um critério de desempate.
internal static class IfoodCatalogResolution
{
    public static async Task<Result<string>> ResolveDefaultCatalogIdAsync(
        string accessToken, string merchantId, IIfoodCatalogClient catalogClient, CancellationToken cancellationToken)
    {
        var catalogs = await catalogClient.GetCatalogsAsync(accessToken, merchantId, cancellationToken);
        if (!catalogs.Success)
            return Result.Failure<string>(new Error("IfoodCatalog.CatalogsFetchFailed", catalogs.ErrorMessage ?? "Falha ao listar os catálogos da loja no Ifood."));

        if (catalogs.Catalogs.Count == 0)
            return Result.Failure<string>(new Error("IfoodCatalog.NoCatalogs", "A loja não tem nenhum catálogo no Ifood."));

        var chosen = catalogs.Catalogs.FirstOrDefault(c => string.Equals(c.Status, "AVAILABLE", StringComparison.OrdinalIgnoreCase))
            ?? catalogs.Catalogs.First();

        if (string.IsNullOrWhiteSpace(chosen.CatalogId))
            return Result.Failure<string>(new Error("IfoodCatalog.NoCatalogId", "O Ifood não retornou um catalogId válido."));

        return Result.Success(chosen.CatalogId);
    }
}
