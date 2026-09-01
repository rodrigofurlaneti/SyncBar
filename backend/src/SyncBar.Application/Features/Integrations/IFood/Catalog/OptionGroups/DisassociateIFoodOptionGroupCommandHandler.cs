using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

internal sealed class DisassociateIfoodOptionGroupCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DisassociateIfoodOptionGroupCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DisassociateIfoodOptionGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DisassociateIfoodOptionGroupCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DisassociateOptionGroupFromProductAsync(token, merchantId, request.OptionGroupId, request.ProductId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.DisassociateOptionGroupFailed", result.ErrorMessage ?? "Falha ao desassociar o grupo de opções do produto no Ifood."));

                return Result.Success();
            });
    }
}
