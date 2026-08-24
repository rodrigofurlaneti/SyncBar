using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Finance.GetScenarios;

internal sealed class GetScenariosQueryHandler(
    ISaleRepository saleRepository,
    IOperatingCostRepository costRepository,
    IStockMovementRepository stockMovementRepository,
    IStockItemRepository stockItemRepository,
    IProductRepository productRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetScenariosQuery, ScenariosResponse>(logRepository, unitOfWork)
{
    private const decimal DefaultMargin = 0.30m;
    private const decimal MinMargin = 0.05m;
    private const decimal MaxMargin = 0.95m;

    public override async Task<Result<ScenariosResponse>> Handle(GetScenariosQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetScenariosQueryHandler),
            nameof(Handle),
            null, // Substitua pelo IP presente no request, se houver
            async (userIdBox) =>
            {
                // Se o seu request possuir o Id do usuário, associe-o aqui:

                var validationError = ValidateRequest(request);
                if (validationError is not null)
                    return Result.Failure<ScenariosResponse>(validationError);

                var from = new DateTime(request.ReferenceYear, request.ReferenceMonth, 1, 0, 0, 0, DateTimeKind.Utc);
                var to = from.AddMonths(1);
                var daysInMonth = DateTime.DaysInMonth(request.ReferenceYear, request.ReferenceMonth);

                var sales = await saleRepository.GetByBranchAndPeriodAsync(request.BranchId, from, to, cancellationToken);
                var costs = await costRepository.GetByBranchAndMonthAsync(
                    request.BranchId, request.ReferenceYear, request.ReferenceMonth, cancellationToken);
                var costOfGoodsSold = await stockMovementRepository.GetSaleCostAsync(request.BranchId, from, to, cancellationToken);
                var soldQuantities = await stockMovementRepository.GetSaleQuantitiesByProductAsync(
                    request.BranchId, from, to, cancellationToken);
                var stockItems = await stockItemRepository.GetByBranchAsync(request.BranchId, cancellationToken);

                var fixedCosts = costs.Where(c => c.CostTypeId == CostTypeIds.Fixo).Sum(c => c.Amount);
                var variableCosts = costs.Where(c => c.CostTypeId == CostTypeIds.Variavel).Sum(c => c.Amount);
                var revenue = sales.Sum(s => s.TotalAmount);

                var (historicalMargin, averageTicket) = CalculateHistoricalMetrics(
                    revenue, sales.Count, costOfGoodsSold, variableCosts);

                var normalMargin = Clamp(request.NormalMargin ?? historicalMargin ?? DefaultMargin);
                var pessimisticMargin = Clamp(request.PessimisticMargin ?? Math.Round(normalMargin * 0.8m, 4));
                var optimisticMargin = Clamp(request.OptimisticMargin ?? Math.Round(normalMargin * 1.2m, 4));

                // Mix de vendas para o plano de estoque.
                var productIds = soldQuantities.Select(q => q.ProductId).ToList();
                var products = await productRepository.GetByIdsAsync(productIds, cancellationToken);
                var mixRevenue = soldQuantities
                    .Select(q => new
                    {
                        q.ProductId,
                        q.Quantity,
                        Product = products.FirstOrDefault(p => p.Id == q.ProductId),
                    })
                    .Where(x => x.Product is not null)
                    .Select(x => new MixRevenueItem(
                        x.ProductId, x.Product!.Name, x.Product.SalePrice, x.Quantity, x.Quantity * x.Product.SalePrice))
                    .ToList();
                var mixTotal = mixRevenue.Sum(x => x.Revenue);

                // Saldo atual por produto, para calcular a necessidade de compra no plano de estoque.
                var currentStockByProduct = stockItems
                    .GroupBy(i => i.ProductId)
                    .ToDictionary(g => g.Key, g => g.First().CurrentQuantity);

                var scenarios = BuildScenarios(
                    request.DesiredProfit, fixedCosts, daysInMonth, averageTicket,
                    mixRevenue, mixTotal, currentStockByProduct,
                    pessimisticMargin, normalMargin, optimisticMargin);

                return Result.Success(new ScenariosResponse(
                    request.ReferenceYear,
                    request.ReferenceMonth,
                    daysInMonth,
                    fixedCosts,
                    request.DesiredProfit,
                    revenue > 0 ? revenue : null,
                    historicalMargin,
                    averageTicket,
                    scenarios));
            });
    }

    private static Error? ValidateRequest(GetScenariosQuery request)
    {
        if (request.ReferenceMonth is < 1 or > 12)
            return new Error("Scenarios.InvalidMonth", "Reference month must be between 1 and 12.");

        if (request.DesiredProfit < 0)
            return new Error("Scenarios.InvalidProfit", "Desired profit cannot be negative.");

        return null;
    }

    // Margem de contribuicao real do mes: 1 - (CMV% + variaveis%).
    private static (decimal? HistoricalMargin, decimal? AverageTicket) CalculateHistoricalMetrics(
        decimal revenue, int salesCount, decimal costOfGoodsSold, decimal variableCosts)
    {
        if (revenue <= 0)
            return (null, null);

        var historicalMargin = Clamp(1 - (costOfGoodsSold + variableCosts) / revenue);
        var averageTicket = salesCount > 0 ? Math.Round(revenue / salesCount, 2) : (decimal?)null;
        return (historicalMargin, averageTicket);
    }

    private static List<ScenarioResponse> BuildScenarios(
        decimal desiredProfit,
        decimal fixedCosts,
        int daysInMonth,
        decimal? averageTicket,
        IReadOnlyCollection<MixRevenueItem> mixRevenue,
        decimal mixTotal,
        IReadOnlyDictionary<long, decimal> currentStockByProduct,
        decimal pessimisticMargin,
        decimal normalMargin,
        decimal optimisticMargin)
    {
        var scenarios = new List<ScenarioResponse>();
        foreach (var (name, margin) in new[]
        {
            ("Pessimista", pessimisticMargin),
            ("Normal", normalMargin),
            ("Otimista", optimisticMargin),
        })
        {
            var breakEven = Math.Round(fixedCosts / margin, 2);
            var target = Math.Round((fixedCosts + desiredProfit) / margin, 2);
            var stockPlan = BuildStockPlan(target, mixTotal, mixRevenue, currentStockByProduct);

            scenarios.Add(new ScenarioResponse(
                name,
                margin,
                breakEven,
                target,
                Math.Round(target / daysInMonth, 2),
                averageTicket is null or 0 ? null : Math.Ceiling(target / averageTicket.Value),
                stockPlan));
        }

        return scenarios;
    }

    // Plano de estoque: distribui o alvo pelo mix real e converte em unidades.
    private static IReadOnlyCollection<StockPlanItemResponse> BuildStockPlan(
        decimal target,
        decimal mixTotal,
        IReadOnlyCollection<MixRevenueItem> mixRevenue,
        IReadOnlyDictionary<long, decimal> currentStockByProduct)
    {
        if (mixTotal <= 0)
            return [];

        return mixRevenue
            .OrderByDescending(x => x.Revenue)
            .Select(x =>
            {
                var share = x.Revenue / mixTotal;
                var units = x.SalePrice <= 0 ? 0 : Math.Ceiling(target * share / x.SalePrice);
                var current = currentStockByProduct.GetValueOrDefault(x.ProductId);
                return new StockPlanItemResponse(
                    x.ProductId, x.Name, Math.Round(share, 4), units, current,
                    Math.Max(0, units - current));
            })
            .ToList();
    }

    private static decimal Clamp(decimal margin)
        => Math.Min(MaxMargin, Math.Max(MinMargin, margin));

    private sealed record MixRevenueItem(long ProductId, string Name, decimal SalePrice, decimal Quantity, decimal Revenue);
}
