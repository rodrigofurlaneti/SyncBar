using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.GetSummary;

internal sealed class GetBillingSummaryQueryHandler(
    ISaleRepository saleRepository,
    IOperatingCostRepository costRepository,
    IRevenueTargetRepository targetRepository,
    IStockMovementRepository stockMovementRepository)
    : IQueryHandler<GetBillingSummaryQuery, BillingSummaryResponse>
{
    public async Task<Result<BillingSummaryResponse>> Handle(
        GetBillingSummaryQuery request, CancellationToken cancellationToken)
    {
        if (request.ReferenceMonth is < 1 or > 12)
            return Result.Failure<BillingSummaryResponse>(
                new Error("BillingSummary.InvalidMonth", "Reference month must be between 1 and 12."));

        var from = new DateTime(request.ReferenceYear, request.ReferenceMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        var sales = await saleRepository.GetByBranchAndPeriodAsync(request.BranchId, from, to, cancellationToken);
        var costs = await costRepository.GetByBranchAndMonthAsync(
            request.BranchId, request.ReferenceYear, request.ReferenceMonth, cancellationToken);
        var target = await targetRepository.GetByBranchAndMonthAsync(
            request.BranchId, request.ReferenceYear, request.ReferenceMonth, cancellationToken);
        var costOfGoodsSold = await stockMovementRepository.GetSaleCostAsync(
            request.BranchId, from, to, cancellationToken);

        var revenue = sales.Sum(s => s.TotalAmount);
        var fixedCosts = costs.Where(c => c.CostTypeId == CostTypeIds.Fixo).Sum(c => c.Amount);
        var variableCosts = costs.Where(c => c.CostTypeId == CostTypeIds.Variavel).Sum(c => c.Amount);
        var totalCosts = fixedCosts + variableCosts + costOfGoodsSold;

        // Ordenacao em C# — extrato diario para o grafico.
        IReadOnlyCollection<DailyRevenueResponse> daily = sales
            .GroupBy(s => s.SoldAt.Day)
            .OrderBy(g => g.Key)
            .Select(g => new DailyRevenueResponse(g.Key, g.Sum(s => s.TotalAmount)))
            .ToList();

        IReadOnlyCollection<OperatingCostResponse> costList = costs
            .OrderBy(c => c.CostTypeId).ThenBy(c => c.Description)
            .Select(c => new OperatingCostResponse(c.Id, c.CostTypeId, c.Description, c.Amount))
            .ToList();

        return Result.Success(new BillingSummaryResponse(
            request.ReferenceYear,
            request.ReferenceMonth,
            revenue,
            sales.Count,
            costOfGoodsSold,
            fixedCosts,
            variableCosts,
            totalCosts,
            revenue - totalCosts,
            target?.TargetAmount,
            target is null || target.TargetAmount == 0 ? null : Math.Round(revenue / target.TargetAmount, 4),
            costList,
            daily));
    }
}
