using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.RegisterPartialPayment;

internal sealed class RegisterPartialPaymentCommandHandler(
    ICustomerOrderRepository orderRepository,
    ICashSessionRepository cashSessionRepository,
    IOrderPartialPaymentRepository partialPaymentRepository,
    IPrintingService printingService,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RegisterPartialPaymentCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(RegisterPartialPaymentCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(RegisterPartialPaymentCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            userIdBox.Value = request.EmployeeId;

            var order = await orderRepository.GetByIdAsync(request.CustomerOrderId, cancellationToken);
            if (order is null || !order.IsActive)
                return Result.Failure<long>(new Error("CustomerOrder.NotFound", "Order not found."));

            if (order.DiningTableId is null)
                return Result.Failure<long>(new Error("PartialPayment.TableOnly",
                    "Pagamento parcial só é permitido em contas de mesa."));

            if (order.OrderStatusId is OrderStatusIds.Pago or OrderStatusIds.Cancelado)
                return Result.Failure<long>(new Error("PartialPayment.OrderClosed", "Order is already settled."));

            var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
            if (session is null || !session.IsActive || !session.IsOpen())
                return Result.Failure<long>(new Error("CashSession.NotOpen", "Cash session is not open."));

            var partials = await partialPaymentRepository.GetByOrderAsync(order.Id, cancellationToken);
            var alreadyPaid = partials.Sum(p => p.Amount);
            var remaining = order.TotalAmount - alreadyPaid;
            if (remaining <= 0)
                return Result.Failure<long>(new Error("PartialPayment.NothingRemaining",
                    "A conta não tem saldo restante para pagamento parcial."));
            if (request.Amount > remaining)
                return Result.Failure<long>(new Error("PartialPayment.ExceedsRemaining",
                    $"Valor ({request.Amount:0.00}) excede o restante da conta ({remaining:0.00})."));

            var partial = OrderPartialPayment.Create(
                order.Id, session.Id, request.PaymentMethodId, request.EmployeeId,
                request.Amount, request.AuthorizationCode, request.PayerName);
            if (partial.IsFailure)
                return Result.Failure<long>(partial.Error);

            await partialPaymentRepository.AddAsync(partial.Value, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                await printingService.PrintPartialReceiptAsync(partial.Value.Id, cancellationToken);
            }
            catch
            {
                // Ignora falhas de impressão
            }

            return Result.Success(partial.Value.Id);
        });
}