using SyncBar.Application.Features.Catalog.Complements;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog;

// Monta os grupos de complemento de cada produto em LOTE (todos os produtos de um cardápio de
// uma vez) — usado por GetMenuQueryHandler (cardápio interno) e GetPublicMenuQueryHandler
// (cardápio do QR Code). Mesmo formato de resposta (ComplementGroupResponse/ComplementResponse)
// já usado pela tela de gestão de Complementos (GetProductComplementGroupsQueryHandler), só que
// resolvido para muitos produtos ao mesmo tempo em vez de um só — evita N+1 query por produto.
internal static class MenuComplementsBuilder
{
    public static async Task<IReadOnlyDictionary<long, IReadOnlyCollection<ComplementGroupResponse>>> BuildAsync(
        IReadOnlyCollection<long> productIds,
        IProductComplementGroupRepository productComplementGroupRepository,
        IComplementGroupRepository complementGroupRepository,
        IComplementItemRepository complementItemRepository,
        CancellationToken cancellationToken)
    {
        if (productIds.Count == 0)
            return new Dictionary<long, IReadOnlyCollection<ComplementGroupResponse>>();

        var links = await productComplementGroupRepository.GetByProductsAsync(productIds, cancellationToken);
        if (links.Count == 0)
            return new Dictionary<long, IReadOnlyCollection<ComplementGroupResponse>>();

        var groupIds = links.Select(l => l.ComplementGroupId).Distinct().ToList();
        var groups = await complementGroupRepository.GetByIdsAsync(groupIds, cancellationToken);
        var groupsById = groups.ToDictionary(g => g.Id);

        var complementItemIds = groups.SelectMany(g => g.Complements).Select(c => c.ComplementItemId).Distinct().ToList();
        var complementItems = complementItemIds.Count > 0
            ? await complementItemRepository.GetByIdsAsync(complementItemIds, cancellationToken)
            : [];
        var complementItemNames = complementItems.ToDictionary(i => i.Id, i => i.Name);

        return links
            .Where(l => groupsById.ContainsKey(l.ComplementGroupId))
            .GroupBy(l => l.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<ComplementGroupResponse>)g
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => ToResponse(groupsById[l.ComplementGroupId], complementItemNames))
                    .ToList());
    }

    private static ComplementGroupResponse ToResponse(ComplementGroup group, IReadOnlyDictionary<long, string> complementItemNames) =>
        new(
            group.Id,
            group.Name,
            group.ComplementGroupTypeId,
            group.MinSelection,
            group.MaxSelection,
            group.IsActive,
            group.Complements
                .Where(c => c.IsActive)
                .Select(c => new ComplementResponse(
                    c.Id,
                    c.ComplementItemId,
                    complementItemNames.TryGetValue(c.ComplementItemId, out var name) ? name : "?",
                    c.ExtraPrice,
                    c.IsActive))
                .ToList());
}
