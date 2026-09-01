using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

internal sealed class DeleteIfoodOptionGroupCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DeleteIfoodOptionGroupCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DeleteIfoodOptionGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DeleteIfoodOptionGroupCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DeleteOptionGroupAsync(token, merchantId, request.OptionGroupId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.DeleteOptionGroupFailed", result.ErrorMessage ?? "Falha ao excluir o grupo de opções no Ifood."));

                return Result.Success();
            });
    }
}
