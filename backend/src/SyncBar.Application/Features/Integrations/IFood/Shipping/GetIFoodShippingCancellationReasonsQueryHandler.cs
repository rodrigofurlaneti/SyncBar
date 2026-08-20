using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodShippingCancellationReasonsQueryHandler(
    IIFoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodShippingCancellationReasonsQuery, IReadOnlyCollection<IFoodShippingCancellationReasonResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodShippingCancellationReasonResponse>>> Handle(
        GetIFoodShippingCancellationReasonsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodShippingCancellationReasonsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodShippingCancellationReasonResponse>>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var reasons = await shippingClient.GetCancellationReasonsAsync(token, delivery.IFoodDeliveryId, cancellationToken);

                IReadOnlyCollection<IFoodShippingCancellationReasonResponse> responses = reasons
                    .Select(r => new IFoodShippingCancellationReasonResponse(r.CancelCodeId, r.Description))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
