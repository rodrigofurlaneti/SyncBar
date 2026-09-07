using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.OrderOrigin.GetAll;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.OrderOrigin.GetById
{
    internal sealed class GetOrderOriginByIdQueryHandler : BaseQueryHandler<GetOrderOriginByIdQuery, OrderOriginResponse>
    {
        private readonly IOrderOriginRepository _orderOriginRepository;

        public GetOrderOriginByIdQueryHandler(
            IOrderOriginRepository orderOriginRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderOriginRepository = orderOriginRepository;
        }

        public override async Task<Result<OrderOriginResponse>> Handle(
            GetOrderOriginByIdQuery request, CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(GetOrderOriginByIdQueryHandler),
                nameof(Handle),
                null,
                async (_) =>
                {
                    var origin = await _orderOriginRepository.GetByIdAsync(request.Id, cancellationToken);

                    if (origin is null)
                    {
                        return Result.Failure<OrderOriginResponse>(
                            new Error("OrderOrigin.NotFound", "Origem de pedido não encontrada."));
                    }

                    var response = new OrderOriginResponse(
                        origin.Id,
                        origin.CompanyId,
                        origin.BranchId,
                        origin.Name,
                        origin.IsActive,
                        origin.CreatedAt
                    );

                    return Result.Success(response);
                });
        }
    }
}
