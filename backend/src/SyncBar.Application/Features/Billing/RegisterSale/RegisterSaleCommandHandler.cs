using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.RegisterSale;

internal sealed class RegisterSaleCommandHandler : BaseCommandHandler<RegisterSaleCommand, long>
{
    private readonly ICustomerOrderRepository _orderRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly ICashSessionRepository _cashSessionRepository;
    private readonly IDiningTableRepository _diningTableRepository;
    private readonly IComandaRepository _comandaRepository;
    private readonly IProductRepository _productRepository;
    private readonly IStockItemRepository _stockItemRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IOrderPartialPaymentRepository _partialPaymentRepository;
    private readonly IPrintingService _printingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _TimeProviderCustom;

    public RegisterSaleCommandHandler(
        ICustomerOrderRepository orderRepository,
        ISaleRepository saleRepository,
        ICashSessionRepository cashSessionRepository,
        IDiningTableRepository diningTableRepository,
        IComandaRepository comandaRepository,
        IProductRepository productRepository,
        IStockItemRepository stockItemRepository,
        IStockMovementRepository stockMovementRepository,
        IOrderPartialPaymentRepository partialPaymentRepository,
        IPrintingService printingService,
        ILogTrackerRepository logRepository,
        IUnitOfWork unitOfWork,
        TimeProvider TimeProviderCustom)
        : base(logRepository, unitOfWork)
    {
        _orderRepository = orderRepository;
        _saleRepository = saleRepository;
        _cashSessionRepository = cashSessionRepository;
        _diningTableRepository = diningTableRepository;
        _comandaRepository = comandaRepository;
        _productRepository = productRepository;
        _stockItemRepository = stockItemRepository;
        _stockMovementRepository = stockMovementRepository;
        _partialPaymentRepository = partialPaymentRepository;
        _printingService = printingService;
        _unitOfWork = unitOfWork;
        _TimeProviderCustom = TimeProviderCustom;
    }

    public override Task<Result<long>> Handle(RegisterSaleCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(RegisterSaleCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            userIdBox.Value = request.EmployeeId;

            var orderResult = await ValidateOrderAsync(request.CustomerOrderId, cancellationToken);
            if (orderResult.IsFailure)
                return Result.Failure<long>(orderResult.Error);
            var order = orderResult.Value;

            var sessionResult = await ValidateCashSessionAsync(request.CashSessionId, cancellationToken);
            if (sessionResult.IsFailure)
                return Result.Failure<long>(sessionResult.Error);
            var session = sessionResult.Value;

            var duplicateCheck = await EnsureNoDuplicateSaleAsync(order.Id, cancellationToken);
            if (duplicateCheck.IsFailure)
                return Result.Failure<long>(duplicateCheck.Error);

            var saleResult = await CreateSaleAsync(order, session, request, cancellationToken);
            if (saleResult.IsFailure)
                return Result.Failure<long>(saleResult.Error);
            var sale = saleResult.Value;

            var paymentsResult = RegisterPayments(sale, request);
            if (paymentsResult.IsFailure)
                return Result.Failure<long>(paymentsResult.Error);

            var fullyPaidResult = await EnsureFullyPaidAsync(sale, order.Id, cancellationToken);
            if (fullyPaidResult.IsFailure)
                return Result.Failure<long>(fullyPaidResult.Error);

            var currentTime = _TimeProviderCustom.GetLocalNow().DateTime;

            var finalizeResult = await FinalizeOrderAsync(order, currentTime, cancellationToken);
            if (finalizeResult.IsFailure)
                return Result.Failure<long>(finalizeResult.Error);

            await DecreaseStockForPaidItemsAsync(order, request.EmployeeId, currentTime, cancellationToken);

            await _saleRepository.AddAsync(sale, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            await PrintReceiptSafelyAsync(sale.Id, cancellationToken);

            return Result.Success(sale.Id);
        });

    private async Task<Result<CustomerOrder>> ValidateOrderAsync(long customerOrderId, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdForUpdateAsync(customerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<CustomerOrder>(new Error("CustomerOrder.NotFound", "Order not found."));
        if (order.OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure<CustomerOrder>(new Error("Sale.OrderNotAwaitingPayment",
                "Close the order before registering the payment."));

        return Result.Success(order);
    }

    private async Task<Result<CashSession>> ValidateCashSessionAsync(long cashSessionId, CancellationToken cancellationToken)
    {
        var session = await _cashSessionRepository.GetByIdAsync(cashSessionId, cancellationToken);
        if (session is null || !session.IsActive || !session.IsOpen())
            return Result.Failure<CashSession>(new Error("CashSession.NotOpen", "Cash session is not open."));

        return Result.Success(session);
    }

    private async Task<Result> EnsureNoDuplicateSaleAsync(long orderId, CancellationToken cancellationToken)
    {
        if (await _saleRepository.ExistsActiveByOrderAsync(orderId, cancellationToken))
            return Result.Failure(new Error("Sale.Duplicate", "Order already has an active sale."));

        return Result.Success();
    }

    private async Task<Result<Sale>> CreateSaleAsync(
        CustomerOrder order, CashSession session, RegisterSaleCommand request, CancellationToken cancellationToken)
    {
        var saleNumber = await _saleRepository.GetNextSaleNumberAsync(order.BranchId, cancellationToken);
        return Sale.Create(
            order.BranchId, order.Id, session.Id, request.EmployeeId, saleNumber,
            order.SubtotalAmount, order.DiscountAmount, order.ServiceFeeAmount);
    }

    private static Result RegisterPayments(Sale sale, RegisterSaleCommand request)
    {
        foreach (var payment in request.Payments)
        {
            var allowsChange = payment.PaymentMethodId == PaymentMethodIds.Dinheiro;
            var added = sale.AddPayment(
                payment.PaymentMethodId, payment.Amount, payment.ChangeAmount,
                payment.AuthorizationCode, allowsChange);
            if (added.IsFailure)
                return added;
        }

        return Result.Success();
    }

    private async Task<Result> EnsureFullyPaidAsync(Sale sale, long orderId, CancellationToken cancellationToken)
    {
        var partials = await _partialPaymentRepository.GetByOrderAsync(orderId, cancellationToken);
        var partiallyPaid = partials.Sum(p => p.Amount);

        return sale.EnsureFullyPaid(partiallyPaid);
    }

    private async Task<Result> FinalizeOrderAsync(CustomerOrder order, DateTime currentTime, CancellationToken cancellationToken)
    {
        var paid = order.MarkAsPaid(currentTime);
        if (paid.IsFailure)
            return paid;

        await ReleaseTableAndComandaAsync(order, cancellationToken);

        return Result.Success();
    }

    private async Task ReleaseTableAndComandaAsync(CustomerOrder order, CancellationToken cancellationToken)
    {
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
    }

    // Baixa de estoque com livro-razao (apenas produtos controlados).
    private async Task DecreaseStockForPaidItemsAsync(
        CustomerOrder order, long employeeId, DateTime currentTime, CancellationToken cancellationToken)
    {
        foreach (var item in order.Items.Where(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado))
        {
            await DecreaseStockForItemAsync(order.BranchId, item, employeeId, currentTime, cancellationToken);
        }
    }

    private async Task DecreaseStockForItemAsync(
        long branchId, OrderItem item, long employeeId, DateTime currentTime, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
        if (product is null || !product.IsStockControlled)
            return;

        var stockItem = await _stockItemRepository.GetByBranchAndProductForUpdateAsync(
            branchId, item.ProductId, cancellationToken);
        if (stockItem is null)
            return;

        var decreased = stockItem.Decrease(item.Quantity);
        if (decreased.IsFailure)
            return;

        var movement = StockMovement.Create(
            stockItem.Id,
            StockMovementTypeIds.SaidaVenda,
            null,
            item.Id,
            employeeId,
            item.Quantity,
            product.CostPrice,
            product.CostPrice is null ? null : Math.Round(product.CostPrice.Value * item.Quantity, 2),
            null, currentTime, null);
        if (movement.IsSuccess)
            await _stockMovementRepository.AddAsync(movement.Value, cancellationToken);
    }

    private async Task PrintReceiptSafelyAsync(long saleId, CancellationToken cancellationToken)
    {
        try
        {
            await _printingService.PrintPaymentReceiptAsync(saleId, cancellationToken);
        }
        catch
        {
            // Ignora falhas de impressão para não quebrar o fluxo da venda
        }
    }
}