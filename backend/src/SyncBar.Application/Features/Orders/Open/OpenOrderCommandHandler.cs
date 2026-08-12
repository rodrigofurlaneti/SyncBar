using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Open;

internal sealed class OpenOrderCommandHandler : BaseCommandHandler<OpenOrderCommand, long>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IComandaRepository _comandaRepository;
    private readonly IComandaSettingRepository _comandaSettingRepository;
    private readonly TimeProvider _timeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public OpenOrderCommandHandler(
        ICustomerOrderRepository orderRepository,
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        IComandaSettingRepository comandaSettingRepository,
        TimeProvider timeProvider,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _comandaSettingRepository = comandaSettingRepository;
        _timeProvider = timeProvider;
        _unitOfWork = unitOfWork;
    }

    public override async Task<Result<long>> Handle(OpenOrderCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(OpenOrderCommandHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, caso aplicável
            async (userIdBox) =>
            {
                // Associa o ID do funcionário/usuário que está abrindo o pedido ao log de auditoria
                userIdBox.Value = request.EmployeeId;

                if (request.DiningTableId.HasValue)
                {
                    var table = await _diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId.Value, cancellationToken);
                    if (table is null || !table.IsActive)
                        return Result.Failure<long>(new Error("DiningTable.NotFound", "Dining table not found."));

                    if (await _orderRepository.HasOpenOrderForTableAsync(request.DiningTableId.Value, cancellationToken))
                        return Result.Failure<long>(new Error("CustomerOrder.TableBusy", "Dining table already has an open order."));

                    table.ChangeStatus(TableStatusIds.Ocupada);
                }

                if (request.ComandaId.HasValue)
                {
                    var comanda = await _comandaRepository.GetByIdForUpdateAsync(request.ComandaId.Value, cancellationToken);
                    if (comanda is null || !comanda.IsActive)
                        return Result.Failure<long>(new Error("Comanda.NotFound", "Comanda not found."));

                    if (await _orderRepository.HasOpenOrderForComandaAsync(request.ComandaId.Value, cancellationToken))
                        return Result.Failure<long>(new Error("CustomerOrder.ComandaBusy", "Comanda already has an open order."));

                    comanda.ChangeStatus(ComandaStatusIds.EmUso);
                }

                decimal? creditLimit = null;
                if (request.ComandaId.HasValue)
                {
                    var setting = await _comandaSettingRepository.GetByBranchAsync(request.BranchId, cancellationToken);
                    creditLimit = setting?.DefaultLimitAmount;
                }

                var currentTime = _timeProvider.GetLocalNow().DateTime;

                var order = CustomerOrder.Create(
                    request.BranchId, request.DiningTableId, request.ComandaId,
                    request.EmployeeId, request.GuestCount, request.Notes, currentTime, creditLimit,
                    request.OrderTypeId, request.CustomerName, request.CustomerPhone, request.DeliveryAddress);

                if (order.IsFailure)
                    return Result.Failure<long>(order.Error);

                await _orderRepository.AddAsync(order.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(order.Value.Id);
            });
    }
}