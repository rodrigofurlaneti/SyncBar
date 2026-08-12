using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.Open;

internal sealed class OpenOrderCommandHandler(
    ICustomerOrderRepository orderRepository,
    IDiningTableRepository diningTableRepository,
    IComandaRepository comandaRepository,
    IComandaSettingRepository comandaSettingRepository,
    TimeProvider timeProvider,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<OpenOrderCommand, long>(logRepository, unitOfWork)
{
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
                    var table = await diningTableRepository.GetByIdForUpdateAsync(request.DiningTableId.Value, cancellationToken);
                    if (table is null || !table.IsActive)
                        return Result.Failure<long>(new Error("DiningTable.NotFound", "Dining table not found."));

                    if (await orderRepository.HasOpenOrderForTableAsync(request.DiningTableId.Value, cancellationToken))
                        return Result.Failure<long>(new Error("CustomerOrder.TableBusy", "Dining table already has an open order."));

                    table.ChangeStatus(TableStatusIds.Ocupada);
                }

                if (request.ComandaId.HasValue)
                {
                    var comanda = await comandaRepository.GetByIdForUpdateAsync(request.ComandaId.Value, cancellationToken);
                    if (comanda is null || !comanda.IsActive)
                        return Result.Failure<long>(new Error("Comanda.NotFound", "Comanda not found."));

                    if (await orderRepository.HasOpenOrderForComandaAsync(request.ComandaId.Value, cancellationToken))
                        return Result.Failure<long>(new Error("CustomerOrder.ComandaBusy", "Comanda already has an open order."));

                    comanda.ChangeStatus(ComandaStatusIds.EmUso);
                }

                decimal? creditLimit = null;
                if (request.ComandaId.HasValue)
                {
                    var setting = await comandaSettingRepository.GetByBranchAsync(request.BranchId, cancellationToken);
                    creditLimit = setting?.DefaultLimitAmount;
                }

                var currentTime = timeProvider.GetLocalNow().DateTime;

                var order = CustomerOrder.Create(
                    request.BranchId, request.DiningTableId, request.ComandaId,
                    request.EmployeeId, request.GuestCount, request.Notes, currentTime, creditLimit,
                    request.OrderTypeId, request.CustomerName, request.CustomerPhone, request.DeliveryAddress);

                if (order.IsFailure)
                    return Result.Failure<long>(order.Error);

                await orderRepository.AddAsync(order.Value, cancellationToken);
                await unitOfWork.CommitAsync(cancellationToken);

                return Result.Success(order.Value.Id);
            });
    }
}