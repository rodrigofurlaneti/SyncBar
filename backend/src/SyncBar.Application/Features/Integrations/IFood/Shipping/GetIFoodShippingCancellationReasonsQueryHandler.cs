using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodShippingCancellationReasonsQueryHandler(
    IIfoodShippingDeliveryRepository deliveryRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodShippingCancellationReasonsQuery, IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>> Handle(
        GetIfoodShippingCancellationReasonsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodShippingCancellationReasonsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodShippingTokenResolution.ResolveAsync(
                    request.Id, deliveryRepository, branchRepository, tokenProvider, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IfoodShippingCancellationReasonResponse>>(resolved.Error);

                var (delivery, token) = resolved.Value;
                var reasons = await shippingClient.GetCancellationReasonsAsync(token, delivery.IfoodDeliveryId, cancellationToken);

                IReadOnlyCollection<IfoodShippingCancellationReasonResponse> responses = reasons
                    .Select(r => new IfoodShippingCancellationReasonResponse(r.CancelCodeId, r.Description))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
