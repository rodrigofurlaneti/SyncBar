using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.GetProductComplementGroups;

internal sealed class GetProductComplementGroupsQueryHandler(
    IProductComplementGroupRepository productComplementGroupRepository,
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
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
                var complementItemNames = complementItems.ToDictionary(i => i.Id, i => i.Name);

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
                                .Select(c => new ComplementResponse(
                                    c.Id,
                                    c.ComplementItemId,
                                    complementItemNames.TryGetValue(c.ComplementItemId, out var name) ? name : "?",
                                    c.ExtraPrice,
                                    c.IsActive))
                                .ToList());
                    })
                    .ToList();

                return Result.Success(response);
            });
}
