using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Financial;

internal sealed class RequestIfoodReconciliationOnDemandCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodFinancialClient financialClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RequestIfoodReconciliationOnDemandCommand, IfoodReconciliationOnDemandResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodReconciliationOnDemandResponse>> Handle(
        RequestIfoodReconciliationOnDemandCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestIfoodReconciliationOnDemandCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodReconciliationOnDemandResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await financialClient.RequestReconciliationOnDemandAsync(token, merchantId, request.Competence, cancellationToken);

                return Result.Success(new IfoodReconciliationOnDemandResponse(result.RequestId, result.RawPayload));
            });
    }
}
