using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodSafeDeliveryScoreQueryHandler(
    IIFoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodSafeDeliveryScoreQuery, IFoodSafeDeliveryScoreResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodSafeDeliveryScoreResponse>> Handle(GetIFoodSafeDeliveryScoreQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodSafeDeliveryScoreQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodSafeDeliveryScoreResponse>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var score = await shippingClient.GetSafeDeliveryScoreAsync(token, delivery.IFoodDeliveryId, cancellationToken);
                if (!score.Success)
                    return Result.Failure<IFoodSafeDeliveryScoreResponse>(new Error("IFoodShipping.SafeDeliveryFailed",
                        score.ErrorMessage ?? "Não foi possível obter o índice de segurança da entrega."));

                return Result.Success(new IFoodSafeDeliveryScoreResponse(score.Score));
            });
    }
}
