using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Orders;

internal sealed class GetIFoodOrderVirtualBagQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodOrderClient orderClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOrderVirtualBagQuery, IFoodOrderVirtualBagResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodOrderVirtualBagResponse>> Handle(
        GetIFoodOrderVirtualBagQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOrderVirtualBagQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure<IFoodOrderVirtualBagResponse>(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IFoodOrderVirtualBagResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IFoodOrderVirtualBagResponse>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var bag = await orderClient.GetVirtualBagAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!bag.Success)
                    return Result.Failure<IFoodOrderVirtualBagResponse>(new Error("IFood.VirtualBagFailed", bag.ErrorMessage ?? "Falha ao buscar a sacola do pedido no iFood."));

                var items = bag.Items.Select(i => new IFoodVirtualBagItemResponse(i.UniqueId, i.Name, i.Quantity, i.Ean)).ToList();

                return Result.Success(new IFoodOrderVirtualBagResponse(
                    bag.Id, bag.ShortCode, bag.Status, bag.CreatedAt, bag.MerchantName, bag.CustomerName,
                    items, bag.GrossValueAmount, bag.GrossValueCurrency, bag.RawPayload));
            });
    }
}
