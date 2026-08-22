using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

internal sealed class DisassociateIFoodOptionGroupCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<DisassociateIFoodOptionGroupCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(DisassociateIFoodOptionGroupCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(DisassociateIFoodOptionGroupCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.DisassociateOptionGroupFromProductAsync(token, merchantId, request.OptionGroupId, request.ProductId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.DisassociateOptionGroupFailed", result.ErrorMessage ?? "Falha ao desassociar o grupo de opções do produto no iFood."));

                return Result.Success();
            });
    }
}
