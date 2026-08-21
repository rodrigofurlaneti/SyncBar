using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class RejectIFoodDisputeCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RejectIFoodDisputeCommand, IFoodDisputeActionResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodDisputeActionResponse>> Handle(RejectIFoodDisputeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RejectIFoodDisputeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IFoodDisputeActionResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IFoodDisputeActionResponse>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var result = await orderClient.RejectDisputeAsync(token, request.DisputeId, request.Reason, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodDisputeActionResponse>(new Error("IFood.ActionFailed", result.ErrorMessage ?? "Falha ao rejeitar a disputa no iFood."));

                return Result.Success(new IFoodDisputeActionResponse(true, result.Status));
            });
    }
}
