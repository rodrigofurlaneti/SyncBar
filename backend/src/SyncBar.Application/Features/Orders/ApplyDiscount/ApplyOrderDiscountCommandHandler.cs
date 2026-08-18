using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.ApplyDiscount;

internal sealed class ApplyOrderDiscountCommandHandler : BaseCommandHandler<ApplyOrderDiscountCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyOrderDiscountCommandHandler(
        ICustomerOrderRepository orderRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _TimeProviderCustom = TimeProviderCustom;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(ApplyOrderDiscountCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ApplyOrderDiscountCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/funcionário responsável pela ação, preencha:
                // userIdBox.Value = request.EmployeeId; // ou request.UserId

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var result = order.ApplyDiscount(request.DiscountAmount, currentTime);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}