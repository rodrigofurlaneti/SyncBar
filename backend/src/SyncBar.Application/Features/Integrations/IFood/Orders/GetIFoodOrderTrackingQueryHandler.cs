using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class GetIFoodOrderTrackingQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOrderTrackingQuery, IFoodOrderTrackingResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodOrderTrackingResponse>> Handle(
        GetIFoodOrderTrackingQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOrderTrackingQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure<IFoodOrderTrackingResponse>(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IFoodOrderTrackingResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IFoodOrderTrackingResponse>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var tracking = await orderClient.GetOrderTrackingAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (tracking is null)
                    return Result.Success(new IFoodOrderTrackingResponse(null, null, null, null, null));

                return Result.Success(new IFoodOrderTrackingResponse(
                    tracking.Latitude, tracking.Longitude, tracking.ExpectedDelivery, tracking.DeliveryEtaEndMinutes, tracking.PickupEtaStartMinutes));
            });
    }
}
