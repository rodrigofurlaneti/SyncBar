using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Billing.GetSalesBySession;

public sealed record SessionSaleResponse(
    long Id,
    long SaleNumber,
    long CustomerOrderId,
    decimal TotalAmount,
    DateTime SoldAt,
    IReadOnlyCollection<string> PaymentSummary);

public sealed record GetSalesBySessionQuery(long CashSessionId) : IQuery<IReadOnlyCollection<SessionSaleResponse>>;
