using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodOrderShippingQuoteQueryHandler(
    IIFoodOrderRepository ifoodOrderRepository,
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOrderShippingQuoteQuery, IFoodShippingQuoteResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodShippingQuoteResponse>> Handle(GetIFoodOrderShippingQuoteQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOrderShippingQuoteQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var ifoodOrder = await ifoodOrderRepository.GetByIdForUpdateAsync(request.IFoodOrderId, cancellationToken);
                if (ifoodOrder is null)
                    return Result.Failure<IFoodShippingQuoteResponse>(new Error("IFoodOrder.NotFound", "Pedido iFood não encontrado."));

                var branch = await branchRepository.GetByIdAsync(ifoodOrder.BranchId, cancellationToken);
                if (branch is null)
                    return Result.Failure<IFoodShippingQuoteResponse>(new Error("Branch.NotFound", "Filial não encontrada."));

                var token = await tokenProvider.GetAccessTokenAsync(branch.CompanyId, cancellationToken);
                if (token is null)
                    return Result.Failure<IFoodShippingQuoteResponse>(new Error("IFood.NotConnected",
                        "Não foi possível autenticar com o iFood — confira as credenciais em Integrações."));

                var quote = await shippingClient.GetDeliveryAvailabilitiesForOrderAsync(token, ifoodOrder.IFoodOrderId, cancellationToken);
                if (!quote.Success || quote.QuoteId is null)
                    return Result.Failure<IFoodShippingQuoteResponse>(new Error("IFoodShipping.QuoteFailed",
                        quote.ErrorMessage ?? "Não foi possível obter cotação de entrega no iFood."));

                return Result.Success(new IFoodShippingQuoteResponse(
                    quote.QuoteId, quote.GrossValue, quote.Discount, quote.NetValue,
                    quote.DeliveryTimeMinMinutes, quote.DeliveryTimeMaxMinutes, quote.DistanceMeters, quote.ExpirationAt));
            });
    }
}
