using SyncBar.Domain.Primitives;

namespace SyncBar.Application.Abstractions.Printing;

public interface IPrintingService
{
    // Disparado no lancamento do pedido — NUNCA lanca excecao nem bloqueia o fluxo.
    Task PrintOrderItemsAsync(long customerOrderId, IReadOnlyCollection<long> orderItemIds, CancellationToken cancellationToken = default);

    Task<Result> PrintBillAsync(long customerOrderId, CancellationToken cancellationToken = default);
    Task<Result> PrintPaymentReceiptAsync(long saleId, CancellationToken cancellationToken = default);
    Task<Result> PrintCashClosingAsync(long cashSessionId, CancellationToken cancellationToken = default);
    Task<Result> PrintTestAsync(long printerId, CancellationToken cancellationToken = default);
}
