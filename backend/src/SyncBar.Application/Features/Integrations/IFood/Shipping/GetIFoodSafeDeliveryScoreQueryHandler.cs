using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodSafeDeliveryScoreQueryHandler(
    IIfoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodSafeDeliveryScoreQuery, IfoodSafeDeliveryScoreResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodSafeDeliveryScoreResponse>> Handle(GetIfoodSafeDeliveryScoreQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodSafeDeliveryScoreQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodSafeDeliveryScoreResponse>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var score = await shippingClient.GetSafeDeliveryScoreAsync(token, delivery.IfoodDeliveryId, cancellationToken);
                if (!score.Success)
                    return Result.Failure<IfoodSafeDeliveryScoreResponse>(new Error("IfoodShipping.SafeDeliveryFailed",
                        score.ErrorMessage ?? "Não foi possível obter o índice de segurança da entrega."));

                return Result.Success(new IfoodSafeDeliveryScoreResponse(score.Score));
            });
    }
}
