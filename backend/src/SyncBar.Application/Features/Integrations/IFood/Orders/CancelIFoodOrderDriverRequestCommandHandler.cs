using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class CancelIFoodOrderDriverRequestCommandHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CancelIFoodOrderDriverRequestCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(CancelIFoodOrderDriverRequestCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CancelIFoodOrderDriverRequestCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var result = await orderClient.CancelOrderRequestDriverAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFood.ActionFailed", result.ErrorMessage ?? "Falha ao cancelar a solicitação de entregador no iFood."));

                return Result.Success();
            });
    }
}
