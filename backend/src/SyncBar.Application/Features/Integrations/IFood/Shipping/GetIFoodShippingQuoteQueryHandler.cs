using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Shipping;

internal sealed class GetIfoodShippingQuoteQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodShippingClient shippingClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodShippingQuoteQuery, IfoodShippingQuoteResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodShippingQuoteResponse>> Handle(GetIfoodShippingQuoteQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodShippingQuoteQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodShippingQuoteResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var quote = await shippingClient.GetDeliveryAvailabilitiesAsync(token, merchantId, request.Latitude, request.Longitude, cancellationToken);
                if (!quote.Success || quote.QuoteId is null)
                    return Result.Failure<IfoodShippingQuoteResponse>(new Error("IfoodShipping.QuoteFailed",
                        quote.ErrorMessage ?? "Não foi possível obter cotação de entrega no Ifood."));

                return Result.Success(new IfoodShippingQuoteResponse(
                    quote.QuoteId, quote.GrossValue, quote.Discount, quote.NetValue,
                    quote.DeliveryTimeMinMinutes, quote.DeliveryTimeMaxMinutes, quote.DistanceMeters, quote.ExpirationAt));
            });
    }
}
