using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Keeta.Order.Create
{
    internal sealed class CreateKeetaIntegrationOrderCommandHandler
        : BaseCommandHandler<CreateKeetaIntegrationOrderCommand, CreateKeetaIntegrationOrderResponse>
    {
        private readonly IKeetaIntegrationOrderRepository _orderRepository;
        private readonly IUnitOfWork _unitOfWork;

        public CreateKeetaIntegrationOrderCommandHandler(
            IKeetaIntegrationOrderRepository orderRepository,
            ILogTrackerRepository logRepository,
            IUnitOfWork unitOfWork)
            : base(logRepository, unitOfWork)
        {
            _orderRepository = orderRepository;
            _unitOfWork = unitOfWork;
        }

        public override async Task<Result<CreateKeetaIntegrationOrderResponse>> Handle(
            CreateKeetaIntegrationOrderCommand request,
            CancellationToken cancellationToken)
        {
            return await ExecuteWithLogAsync(
                nameof(CreateKeetaIntegrationOrderCommandHandler),
                nameof(Handle),
                null,
                async (userIdBox) =>
                {
                    var exists = await _orderRepository.ExistsByKeetaOrderIdAsync(request.KeetaOrderId, cancellationToken);
                    if (exists)
                    {
                        return Result.Failure<CreateKeetaIntegrationOrderResponse>(
                            Error.Conflict(
                                "KeetaOrder.AlreadyExists",
                                $"Já existe um pedido Keeta cadastrado com o KeetaOrderId {request.KeetaOrderId}."));
                    }

                    var orderResult = KeetaIntegrationOrder.Create(
                        request.CompanyId,
                        request.BranchId,
                        request.CustomerId,
                        request.CustomerOrderId,
                        request.KeetaOrderId,
                        request.DisplayId,
                        request.InternalMerchantId,
                        request.KeetaMerchantId,
                        request.OrderType,
                        request.DeliveredBy,
                        request.OrderAmount,
                        request.RawOrderJson,
                        request.OrderCreatedAtUtc);

                    if (orderResult.IsFailure)
                        return Result.Failure<CreateKeetaIntegrationOrderResponse>(orderResult.Error);

                    var order = orderResult.Value;

                    await _orderRepository.AddAsync(order, cancellationToken);
                    await _unitOfWork.CommitAsync(cancellationToken);

                    var response = new CreateKeetaIntegrationOrderResponse(
                        order.Id,
                        order.CompanyId,
                        order.BranchId,
                        order.KeetaOrderId,
                        order.DisplayId,
                        order.Status,
                        order.OrderAmount);

                    return Result.Success(response);
                });
        }
    }
}
