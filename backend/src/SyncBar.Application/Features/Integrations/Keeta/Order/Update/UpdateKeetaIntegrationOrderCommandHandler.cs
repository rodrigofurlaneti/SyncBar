using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Update
{
    internal sealed class UpdateKeetaIntegrationOrderCommandHandler
        : BaseCommandHandler<UpdateKeetaIntegrationOrderCommand>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UpdateKeetaIntegrationOrderCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result> Handle(
            UpdateKeetaIntegrationOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(UpdateKeetaIntegrationOrderCommandHandler),
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

                    if (!string.IsNullOrWhiteSpace(request.Status))
                    {
                        // Usa os métodos de transição específicos quando aplicável, para manter os
                        // timestamps de auditoria (ConfirmedAtUtc/ReadyForPickupAtUtc/ConcludedAtUtc)
                        // consistentes com o histórico do pedido.
                        switch (request.Status.ToUpperInvariant())
                        {
                            case "CONFIRMED":
                                order.MarkAsConfirmed();
                                break;
                            case "READY_FOR_PICKUP":
                                order.MarkAsReadyForPickup();
                                break;
                            case "CONCLUDED":
                                order.MarkAsConcluded();
                                break;
                            default:
                                order.ChangeStatus(request.Status);
                                break;
                        }
                    }

                    _orderRepository.Update(order);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    return Result.Success();
                });
        }
    }
}
