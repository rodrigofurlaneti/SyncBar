using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class GetIFoodCancellationReasonsQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodCancellationReasonsQuery, IReadOnlyCollection<IFoodCancellationReasonResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodCancellationReasonResponse>>> Handle(
        GetIFoodCancellationReasonsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodCancellationReasonsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure<IReadOnlyCollection<IFoodCancellationReasonResponse>>(
                        new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IReadOnlyCollection<IFoodCancellationReasonResponse>>(
                        new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Success<IReadOnlyCollection<IFoodCancellationReasonResponse>>([]);

                var reasons = await orderClient.GetCancellationReasonsAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                IReadOnlyCollection<IFoodCancellationReasonResponse> response = reasons
                    .Select(r => new IFoodCancellationReasonResponse(r.Code, r.Description))
                    .ToList();

                return Result.Success(response);
            });
    }
}
