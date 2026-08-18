using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Close;

internal sealed class CloseOrderCommandHandler : BaseCommandHandler<CloseOrderCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IServiceFeeSettingRepository _serviceFeeSettingRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public CloseOrderCommandHandler(
        ICustomerOrderRepository orderRepository,
        IDiningTableRepository diningTableRepository,
        IServiceFeeSettingRepository serviceFeeSettingRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _diningTableRepository = diningTableRepository;
        _serviceFeeSettingRepository = serviceFeeSettingRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(CloseOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CloseOrderCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/funcionário responsável pela ação, preencha:
                // userIdBox.Value = request.EmployeeId; // ou request.UserId

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var feeSetting = await _serviceFeeSettingRepository.GetByBranchAsync(order.BranchId, cancellationToken);
                var serviceFeeEnabled = feeSetting?.Enabled ?? true;
                var effectiveServiceFeeRate = serviceFeeEnabled ? request.ServiceFeeRate : 0m;

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var result = order.Close(effectiveServiceFeeRate, currentTime);
                if (result.IsFailure)
                    return result;

                if (order.DiningTableId.HasValue)
                {
                    var table = await _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                    table?.ChangeStatus(TableStatusIds.EmFechamento);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}