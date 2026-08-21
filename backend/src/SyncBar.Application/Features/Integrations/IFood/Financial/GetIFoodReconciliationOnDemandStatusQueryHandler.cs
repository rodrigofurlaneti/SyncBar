using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

internal sealed class GetIFoodReconciliationOnDemandStatusQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodFinancialClient financialClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodReconciliationOnDemandStatusQuery, IFoodReconciliationOnDemandStatusResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReconciliationOnDemandStatusResponse>> Handle(
        GetIFoodReconciliationOnDemandStatusQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodReconciliationOnDemandStatusQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReconciliationOnDemandStatusResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var raw = await financialClient.GetReconciliationOnDemandStatusAsync(token, merchantId, request.RequestId, cancellationToken);

                return Result.Success(new IFoodReconciliationOnDemandStatusResponse(raw is not null, raw));
            });
    }
}
