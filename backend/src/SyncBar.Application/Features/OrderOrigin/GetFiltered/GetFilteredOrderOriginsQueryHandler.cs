using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.OrderOrigin.GetFiltered
{
    internal sealed class GetFilteredOrderOriginsQueryHandler
        : BaseQueryHandler<GetFilteredOrderOriginsQuery, IReadOnlyCollection<OrderOriginResponse>>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;

        public GetFilteredOrderOriginsQueryHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
        }

        public override async Task<Result<IReadOnlyCollection<OrderOriginResponse>>> Handle(
            GetFilteredOrderOriginsQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetFilteredOrderOriginsQueryHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var origins = await _orderOriginRepository.GetFilteredAsync(
                        request.CompanyId,
                        request.BranchId,
                        request.SearchTerm,
                        request.IsActive,
                        cancellationToken);

                    var response = origins.Select(o => new OrderOriginResponse(
                        o.Id,
                        o.CompanyId,
                        o.BranchId,
                        o.Name,
                        o.IsActive,
                        o.CreatedAt
                    )).ToList();

                    return Result.Success<IReadOnlyCollection<OrderOriginResponse>>(response);
                });
        }
    }
}
