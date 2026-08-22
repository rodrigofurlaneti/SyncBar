using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

internal sealed class ListIFoodOptionGroupsQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<ListIFoodOptionGroupsQuery, IReadOnlyCollection<IFoodOptionGroupResponse>>(logRepository, unitOfWork)
{
    public override async Task<Result<IReadOnlyCollection<IFoodOptionGroupResponse>>> Handle(
        ListIFoodOptionGroupsQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(ListIFoodOptionGroupsQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IReadOnlyCollection<IFoodOptionGroupResponse>>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.ListOptionGroupsAsync(token, merchantId, request.IncludeOptions, request.CatalogContext, cancellationToken);
                if (!result.Success)
                    return Result.Failure<IReadOnlyCollection<IFoodOptionGroupResponse>>(new Error("IFoodCatalog.OptionGroupsFetchFailed", result.ErrorMessage ?? "Falha ao listar os grupos de opções no iFood."));

                IReadOnlyCollection<IFoodOptionGroupResponse> responses = result.OptionGroups
                    .Select(g => new IFoodOptionGroupResponse(g.Id, g.Name, g.ExternalCode, g.Status, g.Index))
                    .ToList();

                return Result.Success(responses);
            });
    }
}
