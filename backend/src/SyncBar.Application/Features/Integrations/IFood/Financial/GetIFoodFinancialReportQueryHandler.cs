using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

internal sealed class GetIFoodFinancialReportQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodFinancialClient financialClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodFinancialReportQuery, IFoodFinancialReportResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodFinancialReportResponse>> Handle(
        GetIFoodFinancialReportQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodFinancialReportQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodFinancialReportResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;

                IFoodFinancialReportResultDto result;
                if (request.ReportType == IFoodFinancialReportType.AnticipationsV3)
                {
                    result = await financialClient.GetAnticipationsAsync(token, merchantId, cancellationToken);
                }
                else if (request.ReportType == IFoodFinancialReportType.SalesV3)
                {
                    var end = request.RangeEnd ?? DateTime.Today;
                    var start = request.RangeStart ?? end.AddDays(-30);
                    result = await financialClient.GetSalesV3Async(token, merchantId, start, end, page: 1, cancellationToken);
                }
                else
                {
                    result = await financialClient.GetReportAsync(
                        token, merchantId, request.ReportType, request.PeriodId, request.RangeStart, request.RangeEnd, cancellationToken);
                }

                return Result.Success(new IFoodFinancialReportResponse(
                    request.ReportType.ToString(), result.RawItems.Count, result.RawItems));
            });
    }
}
