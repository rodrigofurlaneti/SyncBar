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
    private readonly TimeProvider _TimeProviderCustom;
    private readonly IUnitOfWork _unitOfWork;

    public OpenOrderCommandHandler(
        ICustomerOrderRepository orderRepository,
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        IComandaSettingRepository comandaSettingRepository,
        TimeProvider TimeProviderCustom,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _comandaSettingRepository = comandaSettingRepository;
        _TimeProviderCustom = TimeProviderCustom;
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
                    var tableResult = await ValidateAndOccupyTableAsync(request.DiningTableId.Value, cancellationToken);
                    if (tableResult.IsFailure)
                        return Result.Failure<long>(tableResult.Error);
                }

                if (request.ComandaId.HasValue)
                {
                    var comandaResult = await ValidateAndOccupyComandaAsync(request.ComandaId.Value, cancellationToken);
                    if (comandaResult.IsFailure)
                        return Result.Failure<long>(comandaResult.Error);
                }

                var creditLimit = await GetCreditLimitAsync(request.ComandaId, request.BranchId, cancellationToken);

                var order = CreateOrder(request, creditLimit);
                if (order.IsFailure)
                    return Result.Failure<long>(order.Error);

                await _orderRepository.AddAsync(order.Value, cancellationToken);
                await _unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(order.Value.Id);
            });
    }

    private async Task<Result> ValidateAndOccupyTableAsync(long diningTableId, CancellationToken cancellationToken)
    {
        var table = await _diningTableRepository.GetByIdForUpdateAsync(diningTableId, cancellationToken);
        if (table is null || !table.IsActive)
            return Result.Failure(new Error("DiningTable.NotFound", "Dining table not found."));

        if (await _orderRepository.HasOpenOrderForTableAsync(diningTableId, cancellationToken))
            return Result.Failure(new Error("CustomerOrder.TableBusy", "Dining table already has an open order."));

        table.ChangeStatus(TableStatusIds.Ocupada);
        return Result.Success();
    }

    private async Task<Result> ValidateAndOccupyComandaAsync(long comandaId, CancellationToken cancellationToken)
    {
        var comanda = await _comandaRepository.GetByIdForUpdateAsync(comandaId, cancellationToken);
        if (comanda is null || !comanda.IsActive)
            return Result.Failure(new Error("Comanda.NotFound", "Comanda not found."));

        if (await _orderRepository.HasOpenOrderForComandaAsync(comandaId, cancellationToken))
            return Result.Failure(new Error("CustomerOrder.ComandaBusy", "Comanda already has an open order."));

        comanda.ChangeStatus(ComandaStatusIds.EmUso);
        return Result.Success();
    }

    private async Task<decimal?> GetCreditLimitAsync(long? comandaId, long branchId, CancellationToken cancellationToken)
    {
        if (!comandaId.HasValue)
            return null;

        var setting = await _comandaSettingRepository.GetByBranchAsync(branchId, cancellationToken);
        return setting?.DefaultLimitAmount;
    }

    private Result<CustomerOrder> CreateOrder(OpenOrderCommand request, decimal? creditLimit)
    {
        var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

        return CustomerOrder.Create(
            request.BranchId, request.DiningTableId, request.ComandaId,
            request.EmployeeId, request.GuestCount, request.Notes, currentTime, creditLimit,
            request.OrderTypeId, request.CustomerName, request.CustomerPhone, request.DeliveryAddress);
    }
}