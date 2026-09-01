using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Admin;

internal sealed class UpgradeIfoodCatalogVersionCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<UpgradeIfoodCatalogVersionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(UpgradeIfoodCatalogVersionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpgradeIfoodCatalogVersionCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.UpgradeVersionAsync(token, merchantId, request.CleanMigration, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.UpgradeVersionFailed", result.ErrorMessage ?? "Falha ao migrar o catálogo para a versão v2 no Ifood."));

                return Result.Success();
            });
    }
}
