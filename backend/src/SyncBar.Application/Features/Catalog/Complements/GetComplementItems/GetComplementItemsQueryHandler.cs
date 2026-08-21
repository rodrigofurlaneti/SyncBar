using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Catalog.Complements.GetComplementItems;

internal sealed class GetComplementItemsQueryHandler(
    IComplementItemRepository complementItemRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetComplementItemsQuery, IReadOnlyCollection<ComplementItemResponse>>(logRepository, unitOfWork)
{
    public override Task<Result<IReadOnlyCollection<ComplementItemResponse>>> Handle(
        GetComplementItemsQuery request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(
            nameof(GetComplementItemsQueryHandler),
            nameof(Handle),
            null, // Substitua por request.IpAddress se houver esse campo na Query
            async (userIdBox) =>
            {
                var complementItems = await complementItemRepository.GetByCompanyAsync(request.CompanyId, cancellationToken);

                IReadOnlyCollection<ComplementItemResponse> response = complementItems
                    .OrderBy(c => c.Name)
                    .Select(c => new ComplementItemResponse(c.Id, c.Name, c.IsActive))
                    .ToList();

                return Result.Success(response);
            });
}
