using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class RequestIFoodDisputeAlternativeCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RequestIFoodDisputeAlternativeCommand, IFoodDisputeActionResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodDisputeActionResponse>> Handle(RequestIFoodDisputeAlternativeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestIFoodDisputeAlternativeCommandHandler),
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

                var result = await orderClient.RequestDisputeAlternativeAsync(
                    token, request.DisputeId, request.AlternativeId, request.AlternativeType, request.Amount, request.Currency, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodDisputeActionResponse>(new Error("IFood.ActionFailed", result.ErrorMessage ?? "Falha ao propor a alternativa da disputa no iFood."));

                return Result.Success(new IFoodDisputeActionResponse(true, result.Status));
            });
    }
}
