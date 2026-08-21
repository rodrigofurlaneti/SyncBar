using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class AcceptIFoodDisputeCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<AcceptIFoodDisputeCommand, IFoodDisputeActionResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodDisputeActionResponse>> Handle(AcceptIFoodDisputeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(AcceptIFoodDisputeCommandHandler),
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

                var result = await orderClient.AcceptDisputeAsync(token, request.DisputeId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodDisputeActionResponse>(new Error("IFood.ActionFailed", result.ErrorMessage ?? "Falha ao aceitar a disputa no iFood."));

                return Result.Success(new IFoodDisputeActionResponse(true, result.Status));
            });
    }
}
