using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

internal sealed class ListIfoodCategoryItemsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIfoodCategoryItemsQuery, IfoodCategoryItemsResponse>(logRepository, unitOfWork)
{
    public override async Task<Result<IfoodCategoryItemsResponse>> Handle(
        ListIfoodCategoryItemsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIfoodCategoryItemsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IfoodCategoryItemsResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListCategoryItemsAsync(token, merchantId, request.CategoryId, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IfoodCategoryItemsResponse>(new Error("IfoodCatalog.CategoryItemsFetchFailed", result.ErrorMessage ?? "Falha ao listar os itens da categoria no Ifood."));

                return Result.Success(new IfoodCategoryItemsResponse(result.RawPayload));
            });
    }
}
