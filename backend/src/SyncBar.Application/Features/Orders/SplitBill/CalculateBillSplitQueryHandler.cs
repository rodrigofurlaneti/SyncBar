using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Orders.SplitBill;

internal sealed class CalculateBillSplitQueryHandler(ICustomerOrderRepository orderRepository)
    : IQueryHandler<CalculateBillSplitQuery, BillSplitResponse>
{
    public async Task<Result<BillSplitResponse>> Handle(CalculateBillSplitQuery request, CancellationToken cancellationToken)
    {
        if (request.PeopleCount <= 0)
            return Result.Failure<BillSplitResponse>(
                new Error("BillSplit.InvalidPeopleCount", "People count must be greater than zero."));

        var order = await orderRepository.GetByIdAsync(request.CustomerOrderId, cancellationToken);
        if (order is null || !order.IsActive)
            return Result.Failure<BillSplitResponse>(new Error("CustomerOrder.NotFound", "Order not found."));

        // Divisão em centavos para nunca perder/sobrar 1 centavo por arredondamento —
        // o resto (sempre < PeopleCount centavos) vai para as primeiras pessoas da lista.
        var totalCents = (long)Math.Round(order.TotalAmount * 100, MidpointRounding.AwayFromZero);
        var baseCents = totalCents / request.PeopleCount;
        var remainder = totalCents % request.PeopleCount;

        var shares = new List<BillShareResponse>();
        for (var i = 1; i <= request.PeopleCount; i++)
        {
            var cents = baseCents + (i <= remainder ? 1 : 0);
            shares.Add(new BillShareResponse(i, cents / 100m));
        }

        return Result.Success(new BillSplitResponse(order.TotalAmount, request.PeopleCount, shares));
    }
}
