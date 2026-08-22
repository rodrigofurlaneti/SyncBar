using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Admin;

internal sealed class UpgradeIFoodCatalogVersionCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<UpgradeIFoodCatalogVersionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(UpgradeIFoodCatalogVersionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpgradeIFoodCatalogVersionCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.UpgradeVersionAsync(token, merchantId, request.CleanMigration, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.UpgradeVersionFailed", result.ErrorMessage ?? "Falha ao migrar o catálogo para a versão v2 no iFood."));

                return Result.Success();
            });
    }
}
