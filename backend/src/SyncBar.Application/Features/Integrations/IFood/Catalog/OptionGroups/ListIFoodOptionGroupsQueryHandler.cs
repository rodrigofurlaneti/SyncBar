using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

internal sealed class ListIfoodOptionGroupsQueryHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIfoodOptionGroupsQuery, IReadOnlyCollection<IfoodOptionGroupResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IfoodOptionGroupResponse>>> Handle(
        ListIfoodOptionGroupsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIfoodOptionGroupsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IfoodOptionGroupResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListOptionGroupsAsync(token, merchantId, request.IncludeOptions, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IfoodOptionGroupResponse>>(new Error("IfoodCatalog.OptionGroupsFetchFailed", result.ErrorMessage ?? "Falha ao listar os grupos de opções no Ifood."));

                IReadOnlyCollection<IfoodOptionGroupResponse> responses = result.OptionGroups
                    .Select(g => new IfoodOptionGroupResponse(g.Id, g.Name, g.ExternalCode, g.Status, g.Index))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
