using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;
namespace SyncBar.Application.Features.Integrations.Keeta.Order.ExistsByKeetaOrderId
{
    internal sealed class ExistsKeetaOrderByKeetaOrderIdQueryHandler
        : BaseQueryHandler<ExistsKeetaOrderByKeetaOrderIdQuery, bool>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;

        public ExistsKeetaOrderByKeetaOrderIdQueryHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
        }

        public override async Task<Result<bool>> Handle(
            ExistsKeetaOrderByKeetaOrderIdQuery request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(ExistsKeetaOrderByKeetaOrderIdQueryHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _orderRepository.ExistsByKeetaOrderIdAsync(request.KeetaOrderId, cancellationToken);
                    return Result.Success(exists);
                });
        }
    }
}
