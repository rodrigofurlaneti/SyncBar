using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodShippingTrackingQueryHandler(
    IIFoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodShippingTrackingQuery, IFoodShippingTrackingResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodShippingTrackingResponse>> Handle(GetIFoodShippingTrackingQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodShippingTrackingQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodShippingTrackingResponse>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var tracking = await shippingClient.GetTrackingAsync(token, delivery.IFoodDeliveryId, cancellationToken);
                if (!tracking.Success)
                    return Result.Failure<IFoodShippingTrackingResponse>(new Error("IFoodShipping.TrackingFailed",
                        tracking.ErrorMessage ?? "Não foi possível obter o rastreamento no iFood."));

                return Result.Success(new IFoodShippingTrackingResponse(
                    tracking.Latitude, tracking.Longitude, tracking.ExpectedDelivery, tracking.DeliveryEtaEndMinutes, tracking.PickupEtaStartMinutes));
            });
    }
}
