using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

internal sealed class UpdateIFoodOptionGroupCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<UpdateIFoodOptionGroupCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(UpdateIFoodOptionGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(UpdateIFoodOptionGroupCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.UpdateOptionGroupAsync(token, merchantId, request.OptionGroupId, request.Name, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.UpdateOptionGroupFailed", result.ErrorMessage ?? "Falha ao atualizar o grupo de opções no iFood."));

                return Result.Success();
            });
    }
}
