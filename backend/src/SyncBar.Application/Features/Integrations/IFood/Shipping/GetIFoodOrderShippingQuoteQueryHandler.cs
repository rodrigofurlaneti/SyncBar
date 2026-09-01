using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodOrderShippingQuoteQueryHandler(
    IIfoodOrderRepository IfoodOrderRepository,
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOrderShippingQuoteQuery, IfoodShippingQuoteResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodShippingQuoteResponse>> Handle(GetIfoodOrderShippingQuoteQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOrderShippingQuoteQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var IfoodOrder = await IfoodOrderRepository.GetByIdForUpdateAsync(request.IfoodOrderId, cancellationToken);
                if (IfoodOrder is null)
                    return Result.Failure<IfoodShippingQuoteResponse>(new Error("IfoodOrder.NotFound", "Pedido Ifood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(IfoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IfoodShippingQuoteResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IfoodShippingQuoteResponse>(new Error("Ifood.NotConnected",
                        "Não foi possível autenticar com o Ifood — confira as credenciais em Integrações."));

                var quote = await shippingClient.GetDeliveryAvailabilitiesForOrderAsync(token, IfoodOrder.IfoodOrderId, cancellationToken);
                if (!quote.Success || quote.QuoteId is null)
                    return Result.Failure<IfoodShippingQuoteResponse>(new Error("IfoodShipping.QuoteFailed",
                        quote.ErrorMessage ?? "Não foi possível obter cotação de entrega no Ifood."));

                return Result.Success(new IfoodShippingQuoteResponse(
                    quote.QuoteId, quote.GrossValue, quote.Discount, quote.NetValue,
                    quote.DeliveryTimeMinMinutes, quote.DeliveryTimeMaxMinutes, quote.DistanceMeters, quote.ExpirationAt));
            });
    }
}
