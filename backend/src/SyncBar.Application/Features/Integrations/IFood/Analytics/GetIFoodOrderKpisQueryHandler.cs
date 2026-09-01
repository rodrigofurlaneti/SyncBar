using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Analytics;

internal sealed class GetIfoodOrderKpisQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodAnalyticsClient analyticsClient,
    TimeProvider timeProviderCustom,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodOrderKpisQuery, IfoodOrderKpisResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodOrderKpisResponse>> Handle(
        GetIfoodOrderKpisQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodOrderKpisQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodOrderKpisResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var end = request.PeriodEnd ?? timeProviderCustom.GetLocalNow().DateTime;
                var start = request.PeriodStart ?? end.AddDays(-30);
                var page = request.Page <= 0 ? 1 : request.Page;

                var result = await analyticsClient.GetOrderKpisAsync(token, merchantId, start, end, page, size: 20, cancellationToken);

                return Result.Success(new IfoodOrderKpisResponse(result.CurrentPage, result.RawBuckets));
            });
    }
}
