using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.OrderOrigin.GetByCompanyAndBranch
{
    internal sealed class GetOrderOriginsByCompanyAndBranchQueryHandler
        : BaseQueryHandler<GetOrderOriginsByCompanyAndBranchQuery, IReadOnlyCollection<OrderOriginResponse>>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;

        public GetOrderOriginsByCompanyAndBranchQueryHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
        }

        public override async Task<Result<IReadOnlyCollection<OrderOriginResponse>>> Handle(
            GetOrderOriginsByCompanyAndBranchQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetOrderOriginsByCompanyAndBranchQueryHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var origins = await _orderOriginRepository.GetByCompanyAndBranchAsync(
                        request.CompanyId,
                        request.BranchId,
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
