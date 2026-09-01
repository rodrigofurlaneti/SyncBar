using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Orders;

internal sealed class GetIfoodOrderVirtualBagQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOrderVirtualBagQuery, IfoodOrderVirtualBagResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodOrderVirtualBagResponse>> Handle(
        GetIfoodOrderVirtualBagQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOrderVirtualBagQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<IfoodOrderVirtualBagResponse>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IfoodOrderVirtualBagResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IfoodOrderVirtualBagResponse>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var bag = await orderClient.GetVirtualBagAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (!bag.Success)
                    return Result.Failure<IfoodOrderVirtualBagResponse>(new Error("Ifood.VirtualBagFailed", bag.ErrorMessage ?? "Falha ao buscar a sacola do pedido no Ifood."));

                var items = bag.Items.Select(i => new IfoodVirtualBagItemResponse(i.UniqueId, i.Name, i.Quantity, i.Ean)).ToList();

                return Result.Success(new IfoodOrderVirtualBagResponse(
                    bag.Id, bag.ShortCode, bag.Status, bag.CreatedAt, bag.MerchantName, bag.CustomerName,
                    items, bag.GrossValueAmount, bag.GrossValueCurrency, bag.RawPayload));
            });
    }
}
