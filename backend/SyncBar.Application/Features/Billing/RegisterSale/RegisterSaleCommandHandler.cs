using SyncBar.Application.Abstractions.Messaging;
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
    IUnitOfWork unitOfWork)
    : ICommandHandler<RegisterSaleCommand, long>
{
    public async Task<Result<long>> Handle(RegisterSaleCommand request, CancellationToken cancellationToken)
    {
        // 1. Pedido precisa existir e estar aguardando pagamento (conta fechada).
        var order = await orderRepository.GetByIdForUpdateAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<long>(new Error("CustomerOrder.NotFound", "Order not found."));
        if (order.OrderStatusId != OrderStatusIds.AguardandoPagamento)
            return Result.Failure<long>(new Error("Sale.OrderNotAwaitingPayment",
                "Close the order before registering the payment."));

        // 2. Nunca registrar venda sem caixa aberto.
        var session = await cashSessionRepository.GetByIdAsync(request.CashSessionId, cancellationToken);
        if (session is null || !session.IsActive || !session.IsOpen())
            return Result.Failure<long>(new Error("CashSession.NotOpen", "Cash session is not open."));

        // 3. Uma venda ativa por pedido (espelha UQ_Sale_CustomerOrderId).
        if (await saleRepository.ExistsActiveByOrderAsync(order.Id, cancellationToken))
            return Result.Failure<long>(new Error("Sale.Duplicate", "Order already has an active sale."));

        // 4. Criar a venda com numeracao sequencial por filial.
        var saleNumber = await saleRepository.GetNextSaleNumberAsync(order.BranchId, cancellationToken);
        var saleResult = Sale.Create(
            order.BranchId, order.Id, session.Id, request.EmployeeId, saleNumber,
            order.SubtotalAmount, order.DiscountAmount, order.ServiceFeeAmount);
        if (saleResult.IsFailure)
            return Result.Failure<long>(saleResult.Error);

        var sale = saleResult.Value;

        // 5. Pagamentos multiplos — troco apenas em dinheiro; comprovante no AuthorizationCode.
        foreach (var payment in request.Payments)
        {
            var allowsChange = payment.PaymentMethodId == PaymentMethodIds.Dinheiro;
            var added = sale.AddPayment(
                payment.PaymentMethodId, payment.Amount, payment.ChangeAmount,
                payment.AuthorizationCode, allowsChange);
            if (added.IsFailure)
                return Result.Failure<long>(added.Error);
        }

        var fullyPaid = sale.EnsureFullyPaid();
        if (fullyPaid.IsFailure)
            return Result.Failure<long>(fullyPaid.Error);

        // 6. Pedido pago; mesa/comanda liberadas.
        var paid = order.MarkAsPaid();
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

        // 7. Baixa de estoque com livro-razao (apenas produtos controlados).
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
                continue; // saldo insuficiente nao bloqueia a venda; ajuste de inventario corrige depois

            var movement = StockMovement.Create(
                stockItem.Id, StockMovementTypeIds.SaidaVenda, null, item.Id,
                request.EmployeeId, item.Quantity, product.CostPrice,
                product.CostPrice is null ? null : Math.Round(product.CostPrice.Value * item.Quantity, 2),
                null, DateTime.UtcNow, null);
            if (movement.IsSuccess)
                await stockMovementRepository.AddAsync(movement.Value, cancellationToken);
        }

        await saleRepository.AddAsync(sale, cancellationToken);
        await unitOfWork.CommitAsync(cancellationToken);

        return Result.Success(sale.Id);
    }
}
