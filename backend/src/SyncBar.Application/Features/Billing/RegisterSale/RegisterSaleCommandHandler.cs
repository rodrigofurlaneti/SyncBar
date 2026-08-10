using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Abstractions.Printing;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.RegisterSale;

internal sealed class RegisterSaleCommandHandler(
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
    TimeProvider timeProvider)
    : BaseCommandHandler<RegisterSaleCommand, long>(logRepository, unitOfWork)
{
    public override Task<Result<long>> Handle(RegisterSaleCommand request, CancellationToken cancellationToken) =>
        ExecuteWithLogAsync(nameof(RegisterSaleCommandHandler), nameof(Handle), null, async (userIdBox) =>
        {
            userIdBox.Value = request.EmployeeId;

            var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
            if (order is null || !order.IsActive)
                return Result.Failure<long>(new Error("CustomerOrder.NotFound", "Order not found."));
            if (order.OrderStatusId != OrderStatusIds.AguardandoPagamento)
                return Result.Failure<long>(new Error("Sale.OrderNotAwaitingPayment",
                    "Close the order before registering the payment."));

            var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
            if (session is null || !session.IsActive || !session.IsOpen())
                return Result.Failure<long>(new Error("CashSession.NotOpen", "Cash session is not open."));

            if (await saleRepository.ExistsActiveByOrderAsync(order.Id, cancellationToken))
                return Result.Failure<long>(new Error("Sale.Duplicate", "Order already has an active sale."));

            var saleNumber = await saleRepository.GetNextSaleNumberAsync(order.BranchId, cancellationToken);
            var saleResult = Sale.Create(
                order.BranchId, order.Id, session.Id, request.EmployeeId, saleNumber,
                order.SubtotalAmount, order.DiscountAmount, order.ServiceFeeAmount);
            if (saleResult.IsFailure)
                return Result.Failure<long>(saleResult.Error);

            var sale = saleResult.Value;

            foreach (var payment in request.Payments)
            {
                var allowsChange = payment.PaymentMethodId == PaymentMethodIds.Dinheiro;
                var added = sale.AddPayment(
                    payment.PaymentMethodId, payment.Amount, payment.ChangeAmount,
                    payment.AuthorizationCode, allowsChange);
                if (added.IsFailure)
                    return Result.Failure<long>(added.Error);
            }

            var partials = await partialPaymentRepository.GetByOrderAsync(order.Id, cancellationToken);
            var partiallyPaid = partials.Sum(p => p.Amount);

            var fullyPaid = sale.EnsureFullyPaid(partiallyPaid);
            if (fullyPaid.IsFailure)
                return Result.Failure<long>(fullyPaid.Error);

            var currentTime = timeProvider.GetLocalNow().DateTime;

            var paid = order.MarkAsPaid(currentTime);
            if (paid.IsFailure)
                return Result.Failure<long>(paid.Error);

            if (order.DiningTableId.HasValue)
            {
                var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                table?.ChangeStatus(TableStatusIds.Livre);
            }

            if (order.ComandaId.HasValue)
            {
                var comanda = await comandaRepository.GetByIdForUpdateAsync(order.ComandaId.Value, cancellationToken);
                comanda?.ChangeStatus(ComandaStatusIds.Disponivel);
            }

            // Baixa de estoque com livro-razao (apenas produtos controlados).
            foreach (var item in order.Items.Where(i => i.IsActive && i.OrderItemStatusId != OrderItemStatusIds.Cancelado))
            {
                var product = await productRepository.GetByIdAsync(item.ProductId, cancellationToken);
                if (product is null || !product.IsStockControlled)
                    continue;

                var stockItem = await stockItemRepository.GetByBranchAndProductForUpdateAsync(
                    order.BranchId, item.ProductId, cancellationToken);
                if (stockItem is null)
                    continue;

                var decreased = stockItem.Decrease(item.Quantity);
                if (decreased.IsFailure)
                    continue;

                var movement = StockMovement.Create(
                    stockItem.Id,
                    StockMovementTypeIds.SaidaVenda,
                    null,
                    item.Id,
                    request.EmployeeId,
                    item.Quantity,
                    product.CostPrice,
                    product.CostPrice is null ? null : Math.Round(product.CostPrice.Value * item.Quantity, 2),
                    null, currentTime, null);
                if (movement.IsSuccess)
                    await stockMovementRepository.AddAsync(movement.Value, cancellationToken);
            }

            await saleRepository.AddAsync(sale, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            try
            {
                await printingService.PrintPaymentReceiptAsync(sale.Id, cancellationToken);
            }
            catch
            {
                // Ignora falhas de impressão para não quebrar o fluxo da venda
            }

            return Result.Success(sale.Id);
        });
}