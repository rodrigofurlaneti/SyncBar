using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.Categories;

internal sealed class GetIFoodCatalogsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodCatalogsQuery, IReadOnlyCollection<IFoodCatalogSummaryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodCatalogSummaryResponse>>> Handle(
        GetIFoodCatalogsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIFoodCatalogsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodCatalogSummaryResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetCatalogsAsync(token, merchantId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodCatalogSummaryResponse>>(new Error("IFoodCatalog.CatalogsFetchFailed", result.ErrorMessage ?? "Falha ao listar os catálogos da loja no iFood."));

                IReadOnlyCollection<IFoodCatalogSummaryResponse> responses = result.Catalogs
                    .Select(c => new IFoodCatalogSummaryResponse(c.CatalogId, c.Status, c.Context, c.GroupId, c.ModifiedAt))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
