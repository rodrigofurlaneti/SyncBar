using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.RefundSale;

internal sealed class RefundSaleCommandHandler(
    ISaleRepository saleRepository,
    ICustomerOrderRepository orderRepository,
    ICashSessionRepository cashSessionRepository,
    ICashMovementRepository cashMovementRepository,
    IDiningTableRepository diningTableRepository,
    IComandaRepository comandaRepository,
    IUnitOfWork unitOfWork)
    : ICommandHandler<RefundSaleCommand>
{
    public async Task<Result> Handle(RefundSaleCommand request, CancellationToken cancellationToken)
    {
        var sale = await saleRepository.GetByIdForUpdateAsync(request.SaleId, cancellationToken);
        if (sale is null || !sale.IsActive)
            return Result.Failure(new Error("Sale.NotFound", "Sale not found."));

        // Estorno so com a sessao de caixa ainda ABERTA — depois do fechamento
        // a conferencia ja foi feita e o acerto e contabil.
        var session = await cashSessionRepository.GetByIdAsync(sale.CashSessionId, cancellationToken);
        if (session is null || !session.IsOpen())
            return Result.Failure(new Error("Sale.SessionClosed",
                "A sessão de caixa desta venda já foi fechada — estorno indisponível."));

        // Soft delete da venda: pagamentos saem do esperado do caixa automaticamente.
        sale.Deactivate();

        // Pedido volta a aguardar pagamento; mesa/comanda re-ocupadas.
        var order = await orderRepository.GetByIdForUpdateAsync(sale.CustomerOrderId, cancellationToken);
        if (order is not null)
        {
            var reopened = order.ReopenForPayment();
            if (reopened.IsFailure)
                return reopened;

            if (order.DiningTableId.HasValue)
            {
                var table = await diningTableRepository.GetByIdForUpdateAsync(order.DiningTableId.Value, cancellationToken);
                table?.ChangeStatus(TableStatusIds.EmFechamento);
            }
            if (order.ComandaId.HasValue)
            {
                var comanda = await comandaRepository.GetByIdForUpdateAsync(order.ComandaId.Value, cancellationToken);
                comanda?.ChangeStatus(ComandaStatusIds.EmUso);
            }
        }

        // Trilha de auditoria no caixa (sem impacto no esperado — o calculo ja
        // exclui a venda desativada).
        var movement = CashMovement.Create(
            sale.CashSessionId, CashMovementTypeIds.EstornoVenda, sale.Id,
            request.EmployeeId, sale.TotalAmount,
            string.IsNullOrWhiteSpace(request.Reason) ? $"Estorno da venda #{sale.SaleNumber}" : request.Reason.Trim());
        if (movement.IsSuccess)
            await cashMovementRepository.AddAsync(movement.Value, cancellationToken);

        await unitOfWork.CommitAsync(cancellationToken);
        return Result.Success();
    }
}
