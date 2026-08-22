using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Items;

internal sealed class SetIFoodItemExternalCodeCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIFoodItemExternalCodeCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIFoodItemExternalCodeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIFoodItemExternalCodeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var byCatalog = request.ByCatalog?
                    .Select(c => new IFoodItemExternalCodeByCatalog(c.ExternalCode, c.CatalogContext))
                    .ToList();

                var result = await catalogClient.SetItemExternalCodeAsync(token, merchantId, request.ItemId, request.ExternalCode, byCatalog, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.SetItemExternalCodeFailed", result.ErrorMessage ?? "Falha ao atualizar o código externo do item no iFood."));

                return Result.Success();
            });
    }
}
