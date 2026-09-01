using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.Ifood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Catalog.OptionGroups;

internal sealed class SetIfoodOptionStatusCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIfoodOptionStatusCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIfoodOptionStatusCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIfoodOptionStatusCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IfoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.SetOptionStatusAsync(token, merchantId, request.OptionId, request.Available, request.ParentCustomizationOptionId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IfoodCatalog.SetOptionStatusFailed", result.ErrorMessage ?? "Falha ao atualizar o status da opção no Ifood."));

                return Result.Success();
            });
    }
}
