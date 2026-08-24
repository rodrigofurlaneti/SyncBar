using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.RaiseComandaLimit;

internal sealed class RaiseComandaLimitCommandHandler : BaseCommandHandler<RaiseComandaLimitCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public RaiseComandaLimitCommandHandler(
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

    public override async Task<Result> Handle(RaiseComandaLimitCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RaiseComandaLimitCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário/gerente responsável pela liberação do limite, preencha:

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

                var result = order.RaiseCreditLimit(request.NewLimitAmount, currentTime);
                if (result.IsFailure)
                    return result;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}