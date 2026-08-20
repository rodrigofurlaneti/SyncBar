using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

internal sealed class GetIFoodFinancialSummaryQueryHandler(
    IIFoodFinancialEventRepository financialEventRepository,
    IIFoodSettlementRepository settlementRepository,
    IIFoodOrderRepository ifoodOrderRepository,
    TimeProvider timeProviderCustom,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodFinancialSummaryQuery, IFoodFinancialSummaryResponse>(logRepository, unitOfWork)
{
    // Tolerância de discrepância recomendada pela doc oficial (arredondamentos entre os
    // lançamentos individuais e o repasse consolidado são esperados).
    private const decimal DiscrepancyTolerancePercent = 0.0001m; // 0,01%

    public override async Task<Result<IFoodFinancialSummaryResponse>> Handle(
        GetIFoodFinancialSummaryQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodFinancialSummaryQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var now = timeProviderCustom.GetLocalNow().DateTime;
                var periodEnd = request.To ?? now;
                var periodStart = request.From ?? periodEnd.AddDays(-30);

                var events = await financialEventRepository.GetByBranchAndPeriodAsync(request.BranchId, periodStart, periodEnd, cancellationToken);
                var settlements = await settlementRepository.GetByBranchAndPeriodAsync(request.BranchId, periodStart, periodEnd, cancellationToken);

                var eventResponses = new List<IFoodFinancialEventItemResponse>();
                foreach (var evt in events)
                {
                    long? linkedOrderId = null;
                    if (evt.ReferenceType == "ORDER" && !string.IsNullOrWhiteSpace(evt.ReferenceId))
                    {
                        var linkedOrder = await ifoodOrderRepository.GetByIFoodOrderIdAsync(evt.ReferenceId, cancellationToken);
                        linkedOrderId = linkedOrder?.Id;
                    }

                    eventResponses.Add(new IFoodFinancialEventItemResponse(
                        evt.Id, evt.Name, evt.Description, evt.Amount, evt.HasTransferImpact,
                        evt.CompetenceDate, evt.ReferenceType, evt.ReferenceId, linkedOrderId));
                }

                var settlementResponses = settlements
                    .Select(s => new IFoodSettlementItemResponse(s.Id, s.Type, s.Product, s.Amount, s.Status, s.PaymentDate))
                    .ToList();

                var totalEventsWithImpact = events.Where(e => e.HasTransferImpact).Sum(e => e.Amount);
                var totalSettlements = settlements.Sum(s => s.Amount);
                var discrepancy = Math.Abs(totalEventsWithImpact - totalSettlements);

                // Tolerância relativa ao maior dos dois valores — evita falso-positivo quando
                // ambos são pequenos (ex.: início de operação, poucos pedidos no período).
                var referenceAmount = Math.Max(Math.Abs(totalEventsWithImpact), Math.Abs(totalSettlements));
                var hasDiscrepancy = referenceAmount > 0 && (discrepancy / referenceAmount) > DiscrepancyTolerancePercent;

                var response = new IFoodFinancialSummaryResponse(
                    periodStart, periodEnd, totalEventsWithImpact, totalSettlements, hasDiscrepancy, discrepancy,
                    eventResponses, settlementResponses);

                return Result.Success(response);
            });
    }
}
