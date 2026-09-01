using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Categories;

internal sealed class GetIfoodCatalogsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIfoodCatalogsQuery, IReadOnlyCollection<IfoodCatalogSummaryResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodCatalogSummaryResponse>>> Handle(
        GetIfoodCatalogsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(GetIfoodCatalogsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IfoodCatalogSummaryResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.GetCatalogsAsync(token, merchantId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IfoodCatalogSummaryResponse>>(new Error("IfoodCatalog.CatalogsFetchFailed", result.ErrorMessage ?? "Falha ao listar os catálogos da loja no Ifood."));

                IReadOnlyCollection<IfoodCatalogSummaryResponse> responses = result.Catalogs
                    .Select(c => new IfoodCatalogSummaryResponse(c.CatalogId, c.Status, c.Context, c.GroupId, c.ModifiedAt))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
