using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class GetIfoodCancellationReasonsQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodCancellationReasonsQuery, IReadOnlyCollection<IfoodCancellationReasonResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodCancellationReasonResponse>>> Handle(
        GetIfoodCancellationReasonsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodCancellationReasonsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<IReadOnlyCollection<IfoodCancellationReasonResponse>>(
                        new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IReadOnlyCollection<IfoodCancellationReasonResponse>>(
                        new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Success<IReadOnlyCollection<IfoodCancellationReasonResponse>>([]);

                var reasons = await orderClient.GetCancellationReasonsAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                IReadOnlyCollection<IfoodCancellationReasonResponse> response = reasons
                    .Select(r => new IfoodCancellationReasonResponse(r.Code, r.Description))
                    .ToList();

                return Result.Success(response);
            });
    }
}
