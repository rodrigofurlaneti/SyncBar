using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Delete
{
    internal sealed class DeleteKeetaIntegrationOrderCommandHandler
        : BaseCommandHandler<DeleteKeetaIntegrationOrderCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public DeleteKeetaIntegrationOrderCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            DeleteKeetaIntegrationOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(DeleteKeetaIntegrationOrderCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var order = await _orderRepository.GetByIdForUpdateAsync(request.Id, cancellationToken);

                    if (order is null || order.CompanyId != request.CompanyId)
                    {
                        return Result.Failure(
                            Error.NotFound(
                                "KeetaOrder.NotFound",
                                $"Pedido Keeta com ID {request.Id} não foi encontrado para esta empresa."));
                    }

                    _orderRepository.Delete(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
