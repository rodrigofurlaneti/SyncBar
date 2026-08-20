using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

// Passo comum aos handlers que agem sobre uma IFoodShippingDelivery já criada (tracking,
// cancelamento, safe delivery score): carrega a entrega, resolve a Branch/Company dela e pega um
// access token válido. Diferente de IFoodMerchantResolution (módulo Merchant), aqui NÃO é preciso
// o MerchantId — os endpoints pós-criação usam só o IFoodDeliveryId (id devolvido pelo iFood ao
// pedir o motorista), não merchants/{merchantId}/....
internal static class IFoodShippingTokenResolution
{
    public static async Task<Result<(IFoodShippingDelivery Delivery, string Token)>> ResolveAsync(
        long shippingDeliveryId,
        IIFoodShippingDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIFoodTokenProvider tokenProvider,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryRepository.GetByIdAsync(shippingDeliveryId, cancellationToken);
        if (delivery is null)
            return Result.Failure<(IFoodShippingDelivery, string)>(new Error("IFoodShippingDelivery.NotFound", "Entrega não encontrada."));

        var branch = await branchRepository.GetByIdAsync(delivery.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<(IFoodShippingDelivery, string)>(new Error("Branch.NotFound", "Filial não encontrada."));

        var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
        if (token is null)
            return Result.Failure<(IFoodShippingDelivery, string)>(new Error("IFood.NotConnected",
                "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

        return Result.Success((delivery, token));
    }
}
