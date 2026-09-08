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
    decimal ExpectedCashAmount,
    IReadOnlyCollection<CashMovementResponse>? Movements = null);

public sealed record CashMovementResponse(long Id, long CashMovementTypeId, decimal Amount, string? Description, DateTime CreatedAt);

public sealed record PaymentMethodReconciliationResponse(
    long PaymentMethodId,
    decimal ExpectedAmount,
    decimal CountedAmount,
    decimal DifferenceAmount);

public sealed record CloseCashSessionResponse(
    long CashSessionId,
    decimal ExpectedAmount,
    decimal ClosingAmount,
    decimal DifferenceAmount,
    decimal TotalDifferenceAmount,
    IReadOnlyCollection<PaymentMethodReconciliationResponse> PaymentReconciliations);
