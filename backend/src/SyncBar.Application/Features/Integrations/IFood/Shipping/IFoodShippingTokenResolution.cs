using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

// Passo comum aos handlers que agem sobre uma IfoodShippingDelivery já criada (tracking,
// cancelamento, safe delivery score): carrega a entrega, resolve a Branch/Company dela e pega um
// access token válido. Diferente de IfoodMerchantResolution (módulo Merchant), aqui NÃO é preciso
// o MerchantId — os endpoints pós-criação usam só o IfoodDeliveryId (id devolvido pelo Ifood ao
// pedir o motorista), não merchants/{merchantId}/....
internal static class IfoodShippingTokenResolution
{
    public static async Task<Result<(IfoodShippingDelivery Delivery, string Token)>> ResolveAsync(
        long shippingDeliveryId,
        IIfoodShippingDeliveryRepository deliveryRepository,
        IBranchRepository branchRepository,
        IIfoodTokenProvider tokenProvider,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveryRepository.GetByIdAsync(shippingDeliveryId, cancellationToken);
        if (delivery is null)
            return Result.Failure<(IfoodShippingDelivery, string)>(new Error("IfoodShippingDelivery.NotFound", "Entrega não encontrada."));

        var branch = await branchRepository.GetByIdAsync(delivery.BranchId, cancellationToken);
        if (branch is null)
            return Result.Failure<(IfoodShippingDelivery, string)>(new Error("Branch.NotFound", "Filial não encontrada."));

        var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
        if (token is null)
            return Result.Failure<(IfoodShippingDelivery, string)>(new Error("Ifood.NotConnected",
                "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

        return Result.Success((delivery, token));
    }
}
