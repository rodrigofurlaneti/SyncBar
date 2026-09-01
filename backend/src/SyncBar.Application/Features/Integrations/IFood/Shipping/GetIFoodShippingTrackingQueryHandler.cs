using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodShippingTrackingQueryHandler(
    IIfoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodShippingTrackingQuery, IfoodShippingTrackingResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodShippingTrackingResponse>> Handle(GetIfoodShippingTrackingQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodShippingTrackingQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodShippingTrackingResponse>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var tracking = await shippingClient.GetTrackingAsync(token, delivery.IfoodDeliveryId, cancellationToken);
                if (!tracking.Success)
                    return Result.Failure<IfoodShippingTrackingResponse>(new Error("IfoodShipping.TrackingFailed",
                        tracking.ErrorMessage ?? "Não foi possível obter o rastreamento no Ifood."));

                return Result.Success(new IfoodShippingTrackingResponse(
                    tracking.Latitude, tracking.Longitude, tracking.ExpectedDelivery, tracking.DeliveryEtaEndMinutes, tracking.PickupEtaStartMinutes));
            });
    }
}
