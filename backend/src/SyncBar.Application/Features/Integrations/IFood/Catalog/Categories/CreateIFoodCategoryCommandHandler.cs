using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class CreateIFoodCategoryCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<CreateIFoodCategoryCommand, IFoodCategoryCreateResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IFoodCategoryCreateResponse>> Handle(
        CreateIFoodCategoryCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(CreateIFoodCategoryCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodCategoryCreateResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.CreateCategoryAsync(token, merchantId, request.CatalogId, request.Name, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IFoodCategoryCreateResponse>(new Error("IFoodCatalog.CreateCategoryFailed", result.ErrorMessage ?? "Falha ao criar a categoria no iFood."));

                return Result.Success(new IFoodCategoryCreateResponse(result.IFoodCategoryId));
            });
    }
}
