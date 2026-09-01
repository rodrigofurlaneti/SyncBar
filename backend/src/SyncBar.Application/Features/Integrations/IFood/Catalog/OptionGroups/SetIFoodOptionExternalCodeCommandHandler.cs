using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

internal sealed class SetIfoodOptionExternalCodeCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIfoodOptionExternalCodeCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIfoodOptionExternalCodeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIfoodOptionExternalCodeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.SetOptionExternalCodeAsync(token, merchantId, request.OptionId, request.ExternalCode, request.ParentCustomizationOptionId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.SetOptionExternalCodeFailed", result.ErrorMessage ?? "Falha ao atualizar o código externo da opção no Ifood."));

                return Result.Success();
            });
    }
}
