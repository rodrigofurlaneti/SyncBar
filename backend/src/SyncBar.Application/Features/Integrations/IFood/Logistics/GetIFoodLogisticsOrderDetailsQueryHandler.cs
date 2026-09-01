using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Logistics;

internal sealed class GetIfoodLogisticsOrderDetailsQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodLogisticsClient logisticsClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodLogisticsOrderDetailsQuery, IfoodLogisticsOrderDetailsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodLogisticsOrderDetailsResponse>> Handle(
        GetIfoodLogisticsOrderDetailsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodLogisticsOrderDetailsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<IfoodLogisticsOrderDetailsResponse>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IfoodLogisticsOrderDetailsResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IfoodLogisticsOrderDetailsResponse>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var details = await logisticsClient.GetOrderDetailsAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (!details.Success)
                    return Result.Failure<IfoodLogisticsOrderDetailsResponse>(new Error("Ifood.LogisticsOrderDetailsFailed", details.ErrorMessage ?? "Falha ao buscar os detalhes da entrega no Ifood."));

                return Result.Success(new IfoodLogisticsOrderDetailsResponse(details.RawPayload));
            });
    }
}
