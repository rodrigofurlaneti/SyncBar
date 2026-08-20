using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Shipping;

internal sealed class GetIFoodShippingQuoteQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodShippingQuoteQuery, IFoodShippingQuoteResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodShippingQuoteResponse>> Handle(GetIFoodShippingQuoteQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodShippingQuoteQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodShippingQuoteResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var quote = await shippingClient.GetDeliveryAvailabilitiesAsync(token, merchantId, request.Latitude, request.Longitude, cancellationToken);
                if (!quote.Success || quote.QuoteId is null)
                    return Result.Failure<IFoodShippingQuoteResponse>(new Error("IFoodShipping.QuoteFailed",
                        quote.ErrorMessage ?? "Não foi possível obter cotação de entrega no iFood."));

                return Result.Success(new IFoodShippingQuoteResponse(
                    quote.QuoteId, quote.GrossValue, quote.Discount, quote.NetValue,
                    quote.DeliveryTimeMinMinutes, quote.DeliveryTimeMaxMinutes, quote.DistanceMeters, quote.ExpirationAt));
            });
    }
}
