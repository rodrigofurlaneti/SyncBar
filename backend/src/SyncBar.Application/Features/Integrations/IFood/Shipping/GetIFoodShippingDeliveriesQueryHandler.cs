using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodShippingDeliveriesQueryHandler(
    IIfoodShippingDeliveryRepository deliveryRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodShippingDeliveriesQuery, IReadOnlyCollection<IfoodShippingDeliveryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodShippingDeliveryResponse>>> Handle(
        GetIfoodShippingDeliveriesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodShippingDeliveriesQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var deliveries = await deliveryRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<IfoodShippingDeliveryResponse> responses = deliveries
                    .Select(d => new IfoodShippingDeliveryResponse(
                        d.Id, d.OrderReference, d.CustomerName,
                        $"{d.StreetName}, {d.StreetNumber}{(string.IsNullOrWhiteSpace(d.Complement) ? "" : $" - {d.Complement}")} — {d.Neighborhood}, {d.City}/{d.State}",
                        d.MerchantFee, d.Status, d.TrackingUrl, d.RequestedAt, d.CancelledAt))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
