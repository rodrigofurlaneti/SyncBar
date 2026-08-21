using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Analytics;

internal sealed class GetIFoodOrderKpisQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodAnalyticsClient analyticsClient,
    TimeProvider timeProviderCustom,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodOrderKpisQuery, IFoodOrderKpisResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodOrderKpisResponse>> Handle(
        GetIFoodOrderKpisQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodOrderKpisQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodOrderKpisResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var end = request.PeriodEnd ?? timeProviderCustom.GetLocalNow().DateTime;
                var start = request.PeriodStart ?? end.AddDays(-30);
                var page = request.Page <= 0 ? 1 : request.Page;

                var result = await analyticsClient.GetOrderKpisAsync(token, merchantId, start, end, page, size: 20, cancellationToken);

                return Result.Success(new IFoodOrderKpisResponse(result.CurrentPage, result.RawBuckets));
            });
    }
}
