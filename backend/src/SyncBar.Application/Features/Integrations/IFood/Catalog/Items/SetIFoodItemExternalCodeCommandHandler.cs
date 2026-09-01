using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.Items;

internal sealed class SetIfoodItemExternalCodeCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIfoodItemExternalCodeCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIfoodItemExternalCodeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIfoodItemExternalCodeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var byCatalog = request.ByCatalog?
                    .Select(c => new IfoodItemExternalCodeByCatalog(c.ExternalCode, c.CatalogContext))
                    .ToList();

                var result = await catalogClient.SetItemExternalCodeAsync(token, merchantId, request.ItemId, request.ExternalCode, byCatalog, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.SetItemExternalCodeFailed", result.ErrorMessage ?? "Falha ao atualizar o código externo do item no Ifood."));

                return Result.Success();
            });
    }
}
