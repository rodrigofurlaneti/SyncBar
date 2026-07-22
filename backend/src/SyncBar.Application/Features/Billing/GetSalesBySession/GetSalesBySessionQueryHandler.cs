using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Billing.GetSalesBySession;

internal sealed class GetSalesBySessionQueryHandler(ISaleRepository saleRepository)
    : IQueryHandler<GetSalesBySessionQuery, IReadOnlyCollection<SessionSaleResponse>>
{
    public async Task<Result<IReadOnlyCollection<SessionSaleResponse>>> Handle(
        GetSalesBySessionQuery request, CancellationToken cancellationToken)
    {
        var sales = await saleRepository.GetByCashSessionAsync(request.CashSessionId, cancellationToken);

        IReadOnlyCollection<SessionSaleResponse> response = sales
            .OrderByDescending(s => s.SoldAt)
            .Select(s => new SessionSaleResponse(
                s.Id, s.SaleNumber, s.CustomerOrderId, s.TotalAmount, s.SoldAt,
                s.Payments.Where(p => p.IsActive)
                    .Select(p => $"{p.PaymentMethodId}:{p.Amount:0.00}")
                    .ToList()))
            .ToList();

        return Result.Success(response);
    }
}
