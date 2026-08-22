using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Application.Features.Integrations.IFood.Merchant;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Catalog.OptionGroups;

internal sealed class SetIFoodOptionPriceCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodCatalogClient catalogClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIFoodOptionPriceCommand>(logRepository, unitOfWork)
{
    public override async Task<Result> Handle(SetIFoodOptionPriceCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIFoodOptionPriceCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var result = await catalogClient.SetOptionPriceAsync(token, merchantId, request.OptionId, request.Value, request.OriginalValue, request.ParentCustomizationOptionId, cancellationToken);
                if (!result.Success)
                    return Result.Failure(new Error("IFoodCatalog.SetOptionPriceFailed", result.ErrorMessage ?? "Falha ao atualizar o preço da opção no iFood."));

                return Result.Success();
            });
    }
}
