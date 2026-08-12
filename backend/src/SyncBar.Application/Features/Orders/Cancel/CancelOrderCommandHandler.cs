using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Cancel;

internal sealed class CancelOrderCommandHandler : BaseCommandHandler<CancelOrderCommand>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IComandaRepository _comandaRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelOrderCommandHandler(
        ICustomerOrderRepository orderRepository,
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        TimeProvider timeProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelOrderCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário ou funcionário responsável pela ação, preencha:
                // userIdBox.Value = request.EmployeeId; // ou request.UserId

                var order = await _orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
                if (order is null || !order.IsActive)
                    return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

                var currentTime = _timeProvider.GetLocalNow().DateTime;

                var result = order.Cancel(currentTime);
                if (result.IsFailure)
                    return result;

                // Libera mesa e comanda.
                if (order.DiningTableId.HasValue)
                {
                    var table = await _diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                    table?.ChangeStatus(TableStatusIds.Livre);
                }

                if (order.ComandaId.HasValue)
                {
                    var comanda = await _comandaRepository.GetByIdForUpdateAsync(order.ComandaId.Value, cancellationToken);
                    comanda?.ChangeStatus(ComandaStatusIds.Disponivel);
                }

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}