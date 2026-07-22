namespace SyncBar.Application.Features.Cash;

public sealed record CashSessionResponse(
    long Id,
    long CashRegisterId,
    long CashSessionStatusId,
    long OpenedByEmployeeId,
    decimal OpeningAmount,
    DateTime OpenedAt);

public sealed record PaymentMethodTotalResponse(long PaymentMethodId, decimal TotalAmount);

public sealed record CashSummaryResponse(
    long CashSessionId,
    decimal OpeningAmount,
    int SalesCount,
    decimal SalesTotal,
    IReadOnlyCollection<PaymentMethodTotalResponse> PaymentTotals,
    decimal SuprimentoTotal,
    decimal SangriaTotal,
    decimal DespesaTotal,
    decimal PartialPaymentsTotal,
    decimal ExpectedCashAmount);

public sealed record CloseCashSessionResponse(
    long CashSessionId,
    decimal ExpectedAmount,
    decimal ClosingAmount,
    decimal DifferenceAmount);
