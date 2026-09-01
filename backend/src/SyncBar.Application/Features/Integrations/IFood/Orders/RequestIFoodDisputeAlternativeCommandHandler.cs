using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class RequestIfoodDisputeAlternativeCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RequestIfoodDisputeAlternativeCommand, IfoodDisputeActionResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodDisputeActionResponse>> Handle(RequestIfoodDisputeAlternativeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestIfoodDisputeAlternativeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var branch = await branchRepository.GetByIdAsync(request.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IfoodDisputeActionResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IfoodDisputeActionResponse>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var result = await orderClient.RequestDisputeAlternativeAsync(
                    token, request.DisputeId, request.AlternativeId, request.AlternativeType, request.Amount, request.Currency, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodDisputeActionResponse>(new Error("Ifood.ActionFailed", result.ErrorMessage ?? "Falha ao propor a alternativa da disputa no Ifood."));

                return Result.Success(new IfoodDisputeActionResponse(true, result.Status));
            });
    }
}
