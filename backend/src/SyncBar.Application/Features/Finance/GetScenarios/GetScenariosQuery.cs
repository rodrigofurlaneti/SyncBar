using SyncBar.Application.Abstractions.Messaging;

namespace SyncBar.Application.Features.Finance.GetScenarios;

public sealed record StockPlanItemResponse(
    long ProductId,
    string ProductName,
    decimal RevenueShare,
    decimal EstimatedUnits,
    decimal CurrentStock,
    decimal UnitsToBuy);

public sealed record ScenarioResponse(
    string Name,
    decimal MarginRate,
    decimal BreakEvenRevenue,
    decimal TargetRevenue,
    decimal DailyTarget,
    decimal? EstimatedSalesCount,
    IReadOnlyCollection<StockPlanItemResponse> StockPlan);

public sealed record ScenariosResponse(
    int ReferenceYear,
    int ReferenceMonth,
    int DaysInMonth,
    decimal FixedCosts,
    decimal DesiredProfit,
    decimal? HistoricalRevenue,
    decimal? HistoricalMarginRate,
    decimal? AverageTicket,
    IReadOnlyCollection<ScenarioResponse> Scenarios);

// Margens opcionais (0..1) — sem override, derivam do historico do mes.
public sealed record GetScenariosQuery(
    long BranchId,
    int ReferenceYear,
    int ReferenceMonth,
    decimal DesiredProfit,
    decimal? PessimisticMargin,
    decimal? NormalMargin,
    decimal? OptimisticMargin) : IQuery<ScenariosResponse>;
