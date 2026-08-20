using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Financial;

internal sealed class RequestIFoodReconciliationOnDemandCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodFinancialClient financialClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<RequestIFoodReconciliationOnDemandCommand, IFoodReconciliationOnDemandResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodReconciliationOnDemandResponse>> Handle(
        RequestIFoodReconciliationOnDemandCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(RequestIFoodReconciliationOnDemandCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodReconciliationOnDemandResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await financialClient.RequestReconciliationOnDemandAsync(token, merchantId, request.Competence, cancellationToken);

                return Result.Success(new IFoodReconciliationOnDemandResponse(result.RequestId, result.RawPayload));
            });
    }
}
