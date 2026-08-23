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
        CancellationToken cancellationToken,
        // Fase 18 (combos) — resolve a imagem dos produtos vinculados
        // (ComplementItem.LinkedProductId) pra exibir no cardápio (interno/QR Code) em vez de só o nome.
        IProductRepository productRepository)
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
        var complementItemsById = complementItems.ToDictionary(i => i.Id);

        IReadOnlyDictionary<long, string?> linkedProductImages = new Dictionary<long, string?>();
        var linkedProductIds = complementItems
            .Where(i => i.LinkedProductId.HasValue)
            .Select(i => i.LinkedProductId!.Value)
            .Distinct()
            .ToList();
        if (linkedProductIds.Count > 0)
        {
            var linkedProducts = await productRepository.GetByIdsAsync(linkedProductIds, cancellationToken);
            linkedProductImages = linkedProducts.ToDictionary(p => p.Id, p => p.ImageUrl);
        }

        return links
            .Where(l => groupsById.ContainsKey(l.ComplementGroupId))
            .GroupBy(l => l.ProductId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyCollection<ComplementGroupResponse>)g
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l => ToResponse(groupsById[l.ComplementGroupId], complementItemsById, linkedProductImages))
                    .ToList());
    }

    private static ComplementGroupResponse ToResponse(
        ComplementGroup group,
        IReadOnlyDictionary<long, ComplementItem> complementItemsById,
        IReadOnlyDictionary<long, string?> linkedProductImages) =>
        new(
            group.Id,
            group.Name,
            group.ComplementGroupTypeId,
            group.MinSelection,
            group.MaxSelection,
            group.IsActive,
            group.Complements
                .Where(c => c.IsActive)
                .Select(c =>
                {
                    complementItemsById.TryGetValue(c.ComplementItemId, out var complementItem);
                    var linkedProductId = complementItem?.LinkedProductId;
                    var linkedImageUrl = linkedProductId.HasValue
                        ? linkedProductImages.GetValueOrDefault(linkedProductId.Value)
                        : null;
                    return new ComplementResponse(
                        c.Id,
                        c.ComplementItemId,
                        complementItem?.Name ?? "?",
                        c.ExtraPrice,
                        c.IsActive,
                        linkedProductId,
                        linkedImageUrl);
                })
                .ToList());
}
