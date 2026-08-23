using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;

internal sealed class GetProductComplementGroupsQueryHandler(
    IProductComplementGroupRepository productComplementGroupRepository,
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetProductComplementGroupsQuery, IReadOnlyCollection<ProductComplementGroupResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ProductComplementGroupResponse>>> Handle(
        GetProductComplementGroupsQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetProductComplementGroupsQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se houver esse campo na Query
            async (userIdBox) =>
            {
                var links = await productComplementGroupRepository.GetByProductAsync(request.ProductId, cancellationToken);
                if (links.Count == 0)
                    return Result.Success<IReadOnlyCollection<ProductComplementGroupResponse>>([]);

                var groupIds = links.Select(l => l.ComplementGroupId).Distinct().ToList();
                var groups = await complementGroupRepository.GetByIdsAsync(groupIds, cancellationToken);
                var groupsById = groups.ToDictionary(g => g.Id);

                var complementItemIds = groups.SelectMany(g => g.Complements).Select(c => c.ComplementItemId).Distinct().ToList();
                var complementItems = await complementItemRepository.GetByIdsAsync(complementItemIds, cancellationToken);
                var complementItemsById = complementItems.ToDictionary(i => i.Id);

                // Fase 18 (combos) — resolve a imagem dos produtos vinculados (LinkedProductId) em
                // lote, mesmo critério de N+1 evitado em MenuComplementsBuilder.
                var linkedProductIds = complementItems
                    .Where(i => i.LinkedProductId.HasValue)
                    .Select(i => i.LinkedProductId!.Value)
                    .Distinct()
                    .ToList();
                var linkedProducts = linkedProductIds.Count > 0
                    ? await productRepository.GetByIdsAsync(linkedProductIds, cancellationToken)
                    : (IReadOnlyCollection<Product>)[];
                var linkedProductImages = linkedProducts.ToDictionary(p => p.Id, p => p.ImageUrl);

                IReadOnlyCollection<ProductComplementGroupResponse> response = links
                    .Where(l => groupsById.ContainsKey(l.ComplementGroupId))
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l =>
                    {
                        var group = groupsById[l.ComplementGroupId];
                        return new ProductComplementGroupResponse(
                            l.Id,
                            group.Id,
                            group.Name,
                            group.ComplementGroupTypeId,
                            group.MinSelection,
                            group.MaxSelection,
                            l.DisplayOrder,
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
                    })
                    .ToList();

                return Result.Success(response);
            });
}
