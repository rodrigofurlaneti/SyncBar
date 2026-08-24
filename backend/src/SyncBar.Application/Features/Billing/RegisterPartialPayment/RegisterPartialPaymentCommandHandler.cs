using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.RegisterPartialPayment;

internal sealed class RegisterPartialPaymentCommandHandler : BaseCommandHandler<RegisterPartialPaymentCommand, long>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository;
    private readonly IPrintingService _printingService;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPartialPaymentCommandHandler(
        ICustomerOrderRepository orderRepository,
        ICashSessionRepository cashSessionRepository,
        IOrderPartialPaymentRepository partialPaymentRepository,
        IPrintingService printingService,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _cashSessionRepository = cashSessionRepository;
        _partialPaymentRepository = partialPaymentRepository;
        _printingService = printingService;
        _unitOfWork = unitOfWork;
    }

    public override Task<Result<long>> Handle(RegisterPartialPaymentCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(RegisterPartialPaymentCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            userIdBox.Value = request.EmployeeId;

            var order = await _orderRepository.GetByIdAsync(request.CustomerOrderId, cancellationToken);
            var orderValidation = ValidateOrder(order);
            if (orderValidation.IsFailure)
                return Result.Failure<long>(orderValidation.Error);

            var session = await _cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
            var sessionValidation = ValidateCashSession(session);
            if (sessionValidation.IsFailure)
                return Result.Failure<long>(sessionValidation.Error);

            var partials = await _partialPaymentRepository.GetByOrderAsync(order!.Id, cancellationToken);
            var remainingValidation = ValidateRemainingAmount(order, partials, request.Amount);
            if (remainingValidation.IsFailure)
                return Result.Failure<long>(remainingValidation.Error);

            var partial = OrderPartialPayment.Create(
                order.Id, session!.Id, request.PaymentMethodId, request.EmployeeId,
                request.Amount, request.AuthorizationCode, request.PayerName);
            if (partial.IsFailure)
                return Result.Failure<long>(partial.Error);

            await _partialPaymentRepository.AddAsync(partial.Value, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await TryPrintPartialReceiptAsync(partial.Value.Id, cancellationToken);

            return Result.Success(partial.Value.Id);
        });

    private static Result ValidateOrder(CustomerOrder? order)
    {
        if (order is null || !order.IsActive)
            return Result.Failure(new Error("CustomerOrder.NotFound", "Order not found."));

        if (order.DiningTableId is null)
            return Result.Failure(new Error("PartialPayment.TableOnly",
                "Pagamento parcial só é permitido em contas de mesa."));

        if (order.OrderStatusId is OrderStatusIds.Pago or OrderStatusIds.Cancelado)
            return Result.Failure(new Error("PartialPayment.OrderClosed", "Order is already settled."));

        return Result.Success();
    }

    private static Result ValidateCashSession(CashSession? session)
    {
        if (session is null || !session.IsActive || !session.IsOpen())
            return Result.Failure(new Error("CashSession.NotOpen", "Cash session is not open."));

        return Result.Success();
    }

    private static Result ValidateRemainingAmount(
        CustomerOrder order, IEnumerable<OrderPartialPayment> partials, decimal requestedAmount)
    {
        var alreadyPaid = partials.Sum(p => p.Amount);
        var remaining = order.TotalAmount - alreadyPaid;

        if (remaining <= 0)
            return Result.Failure(new Error("PartialPayment.NothingRemaining",
                "A conta não tem saldo restante para pagamento parcial."));

        if (requestedAmount > remaining)
            return Result.Failure(new Error("PartialPayment.ExceedsRemaining",
                $"Valor ({requestedAmount:0.00}) excede o restante da conta ({remaining:0.00})."));

        return Result.Success();
    }

    private async Task TryPrintPartialReceiptAsync(long partialPaymentId, CancellationToken cancellationToken)
    {
        try
        {
            await _printingService.PrintPartialReceiptAsync(partialPaymentId, cancellationToken);
        }
        catch
        {
            // Ignora falhas de impressão
        }
    }
}