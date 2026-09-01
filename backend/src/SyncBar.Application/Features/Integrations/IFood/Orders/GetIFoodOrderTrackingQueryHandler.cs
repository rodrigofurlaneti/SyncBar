using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class GetIfoodOrderTrackingQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOrderTrackingQuery, IfoodOrderTrackingResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodOrderTrackingResponse>> Handle(
        GetIfoodOrderTrackingQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOrderTrackingQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<IfoodOrderTrackingResponse>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IfoodOrderTrackingResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IfoodOrderTrackingResponse>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var tracking = await orderClient.GetOrderTrackingAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (tracking is null)
                    return Result.Success(new IfoodOrderTrackingResponse(null, null, null, null, null));

                return Result.Success(new IfoodOrderTrackingResponse(
                    tracking.Latitude, tracking.Longitude, tracking.ExpectedDelivery, tracking.DeliveryEtaEndMinutes, tracking.PickupEtaStartMinutes));
            });
    }
}
