using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class ValidateIfoodPickupCodeCommandHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<ValidateIfoodPickupCodeCommand, bool>(logRepository, unitOfWork)
{
    public override async Task<Result<bool>> Handle(ValidateIfoodPickupCodeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ValidateIfoodPickupCodeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<bool>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<bool>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<bool>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var result = await orderClient.ValidatePickupCodeAsync(token, IfoodOrder.IfoodOrderId, request.Code, cancellationToken);
                if (!result.Success)
                    return Result.Failure<bool>(new Error("Ifood.ActionFailed", result.ErrorMessage ?? "Falha ao validar o código de retirada no Ifood."));

                return Result.Success(result.CodeMatched);
            });
    }
}
