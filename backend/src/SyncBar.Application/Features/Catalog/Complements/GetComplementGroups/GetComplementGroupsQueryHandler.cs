using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;

internal sealed class GetComplementGroupsQueryHandler(
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetComplementGroupsQuery, IReadOnlyCollection<ComplementGroupResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ComplementGroupResponse>>> Handle(
        GetComplementGroupsQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetComplementGroupsQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se houver esse campo na Query
            async (userIdBox) =>
            {
                var complementGroups = await complementGroupRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                var complementItemIds = complementGroups
                    .SelectMany(g => g.Complements)
                    .Select(c => c.ComplementItemId)
                    .Distinct()
                    .ToList();
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

                IReadOnlyCollection<ComplementGroupResponse> response = complementGroups
                    .OrderBy(g => g.Name)
                    .Select(g => new ComplementGroupResponse(
                        g.Id,
                        g.Name,
                        g.ComplementGroupTypeId,
                        g.MinSelection,
                        g.MaxSelection,
                        g.IsActive,
                        g.Complements
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
                            .ToList()))
                    .ToList();

                return Result.Success(response);
            });
}
