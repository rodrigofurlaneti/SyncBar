using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class GetIFoodBatchResultQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodBatchResultQuery, IFoodBatchStatusResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodBatchStatusResponse>> Handle(
        GetIFoodBatchResultQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodBatchResultQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodBatchStatusResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetBatchResultAsync(token, merchantId, request.BatchId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodBatchStatusResponse>(new Error("IFoodCatalog.BatchResultFetchFailed", result.ErrorMessage ?? "Falha ao consultar o resultado do lote no iFood."));

                var items = result.Results
                    .Select(r => new IFoodBatchResultItemResponse(r.ResourceId, r.Result, r.FailureReason))
                    .ToList();

                return Result.Success(new IFoodBatchStatusResponse(result.BatchStatus, items));
            });
    }
}
