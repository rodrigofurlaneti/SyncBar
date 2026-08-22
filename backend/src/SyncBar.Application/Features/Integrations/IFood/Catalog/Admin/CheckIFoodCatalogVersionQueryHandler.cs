using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class CheckIFoodCatalogVersionQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<CheckIFoodCatalogVersionQuery, IFoodCatalogVersionResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCatalogVersionResponse>> Handle(
        CheckIFoodCatalogVersionQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CheckIFoodCatalogVersionQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCatalogVersionResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.CheckVersionAsync(token, merchantId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodCatalogVersionResponse>(new Error("IFoodCatalog.VersionCheckFailed", result.ErrorMessage ?? "Falha ao consultar a versão do catálogo no iFood."));

                return Result.Success(new IFoodCatalogVersionResponse(result.Version));
            });
    }
}
