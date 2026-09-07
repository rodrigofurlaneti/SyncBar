using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.OrderOrigin.GetAll
{
    internal sealed class GetAllOrderOriginsQueryHandler : BaseQueryHandler<GetAllOrderOriginsQuery, IReadOnlyCollection<OrderOriginResponse>>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;

        public GetAllOrderOriginsQueryHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
        }

        public override async Task<Result<IReadOnlyCollection<OrderOriginResponse>>> Handle(
            GetAllOrderOriginsQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetAllOrderOriginsQueryHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var origins = await _orderOriginRepository.GetAllAsync(cancellationToken);

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
