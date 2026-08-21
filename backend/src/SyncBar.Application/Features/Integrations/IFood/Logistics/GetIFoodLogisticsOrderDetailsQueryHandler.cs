using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Logistics;

internal sealed class GetIFoodLogisticsOrderDetailsQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodLogisticsClient logisticsClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodLogisticsOrderDetailsQuery, IFoodLogisticsOrderDetailsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodLogisticsOrderDetailsResponse>> Handle(
        GetIFoodLogisticsOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodLogisticsOrderDetailsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure<IFoodLogisticsOrderDetailsResponse>(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IFoodLogisticsOrderDetailsResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IFoodLogisticsOrderDetailsResponse>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var details = await logisticsClient.GetOrderDetailsAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!details.Success)
                    return Result.Failure<IFoodLogisticsOrderDetailsResponse>(new Error("IFood.LogisticsOrderDetailsFailed", details.ErrorMessage ?? "Falha ao buscar os detalhes da entrega no iFood."));

                return Result.Success(new IFoodLogisticsOrderDetailsResponse(details.RawPayload));
            });
    }
}
