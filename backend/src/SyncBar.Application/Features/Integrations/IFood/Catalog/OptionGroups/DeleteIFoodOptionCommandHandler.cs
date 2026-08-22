using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

internal sealed class DeleteIFoodOptionCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIFoodOptionCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIFoodOptionCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIFoodOptionCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteOptionAsync(token, merchantId, request.OptionGroupId, request.ProductId, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DeleteOptionFailed", result.ErrorMessage ?? "Falha ao excluir a opção no iFood."));

                return Result.Success();
            });
    }
}
