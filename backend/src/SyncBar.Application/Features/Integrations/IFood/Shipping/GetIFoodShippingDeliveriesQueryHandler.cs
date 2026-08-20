using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodShippingDeliveriesQueryHandler(
    IIFoodShippingDeliveryRepository deliveryRepository,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodShippingDeliveriesQuery, IReadOnlyCollection<IFoodShippingDeliveryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodShippingDeliveryResponse>>> Handle(
        GetIFoodShippingDeliveriesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodShippingDeliveriesQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var deliveries = await deliveryRepository.GetOpenByBranchAsync(request.BranchId, cancellationToken);

                IReadOnlyCollection<IFoodShippingDeliveryResponse> responses = deliveries
                    .Select(d => new IFoodShippingDeliveryResponse(
                        d.Id, d.OrderReference, d.CustomerName,
                        $"{d.StreetName}, {d.StreetNumber}{(string.IsNullOrWhiteSpace(d.Complement) ? "" : $" - {d.Complement}")} — {d.Neighborhood}, {d.City}/{d.State}",
                        d.MerchantFee, d.Status, d.TrackingUrl, d.RequestedAt, d.CancelledAt))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
