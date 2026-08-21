using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.GetComplementGroups;

internal sealed class GetComplementGroupsQueryHandler(
    IComplementGroupRepository complementGroupRepository,
    IComplementItemRepository complementItemRepository,
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
                var complementItemNames = complementItems.ToDictionary(i => i.Id, i => i.Name);

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
                            .Select(c => new ComplementResponse(
                                c.Id,
                                c.ComplementItemId,
                                complementItemNames.TryGetValue(c.ComplementItemId, out var name) ? name : "?",
                                c.ExtraPrice,
                                c.IsActive))
                            .ToList()))
                    .ToList();

                return Result.Success(response);
            });
}
