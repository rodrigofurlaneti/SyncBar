namespace SyncBar.Application.Features.Finance;

public sealed record OperatingCostResponse(
    long Id,
    long CostTypeId,
    string Description,
    decimal Amount);

public sealed record DailyRevenueResponse(int Day, decimal Amount);

public sealed record BillingSummaryResponse(
    int ReferenceYear,
    int ReferenceMonth,
    decimal Revenue,
    int SalesCount,
    decimal CostOfGoodsSold,
    decimal FixedCosts,
    decimal VariableCosts,
    decimal TotalCosts,
    decimal NetResult,
    decimal? TargetAmount,
    decimal? TargetAttainmentRate,
    IReadOnlyCollection<OperatingCostResponse> Costs,
    IReadOnlyCollection<DailyRevenueResponse> DailyRevenue);
