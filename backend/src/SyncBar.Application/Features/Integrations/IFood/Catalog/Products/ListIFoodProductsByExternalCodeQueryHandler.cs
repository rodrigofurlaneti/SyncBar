using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Products;

internal sealed class ListIfoodProductsByExternalCodeQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIfoodProductsByExternalCodeQuery, IReadOnlyCollection<IfoodProductResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodProductResponse>>> Handle(
        ListIfoodProductsByExternalCodeQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIfoodProductsByExternalCodeQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IfoodProductResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListProductsByExternalCodeAsync(token, merchantId, request.ExternalCode, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IfoodProductResponse>>(new Error("IfoodCatalog.ProductsByExternalCodeFetchFailed", result.ErrorMessage ?? "Falha ao listar os produtos por código externo no Ifood."));

                IReadOnlyCollection<IfoodProductResponse> responses = result.Products
                    .Select(p => new IfoodProductResponse(p.Id, p.Name, p.Description, p.AdditionalInformation, p.ExternalCode, p.Ean, p.Industrialized, p.ImagePath))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
