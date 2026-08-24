using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Reopen;

internal sealed class ReopenOrderCommandHandler : BaseCommandHandler<ReopenOrderCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenOrderCommandHandler(
        ICustomerOrderRepository orderRepository,
        IDiningTableRepository diningTableRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _diningTableRepository = diningTableRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(ReopenOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ReopenOrderCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável pela reabertura, preencha:

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var result = order.ReopenForConsumption(currentTime);
                if (result.IsFailure)
                    return result;

                if (order.DiningTableId.HasValue)
                {
                    var table = await _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                    table?.ChangeStatus(TableStatusIds.Ocupada);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}