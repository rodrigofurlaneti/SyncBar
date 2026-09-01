using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

internal sealed class GetIfoodReconciliationOnDemandStatusQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodFinancialClient financialClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodReconciliationOnDemandStatusQuery, IfoodReconciliationOnDemandStatusResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReconciliationOnDemandStatusResponse>> Handle(
        GetIfoodReconciliationOnDemandStatusQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodReconciliationOnDemandStatusQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReconciliationOnDemandStatusResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var raw = await financialClient.GetReconciliationOnDemandStatusAsync(token, merchantId, request.RequestId, cancellationToken);

                return Result.Success(new IfoodReconciliationOnDemandStatusResponse(raw is not null, raw));
            });
    }
}
