using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

// Único fluxo do módulo Merchant que exige o header X-iFood-Customer-ID (ver comentário em
// IIFoodMerchantClient) — sem IFoodIntegrationSetting.IFoodCustomerId configurado, retorna erro
// amigável em vez de tentar a chamada (que falharia no iFood de qualquer forma).
internal sealed class SetIFoodPreparationTimeCommandHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIFoodPreparationTimeCommand>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task<Result> Handle(SetIFoodPreparationTimeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIFoodPreparationTimeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure(resolved.Error);

                var (_, merchantId, token, ifoodCustomerId) = resolved.Value;
                if (string.IsNullOrWhiteSpace(ifoodCustomerId))
                    return Result.Failure(new Error(
                        "IFoodMerchant.MissingCustomerId",
                        "Configure o iFood Customer ID nas credenciais do app antes de definir o tempo de preparo."));

                if (request.Minutes is null)
                {
                    var deleteResult = await merchantClient.DeletePreparationTimeAsync(token, merchantId, ifoodCustomerId, cancellationToken);
                    if (!deleteResult.Success)
                        return Result.Failure(new Error("IFoodMerchant.DeletePreparationTimeFailed", deleteResult.ErrorMessage ?? "Failed to reset preparation time on iFood."));
                }
                else
                {
                    var upsertResult = await merchantClient.UpsertPreparationTimeAsync(token, merchantId, ifoodCustomerId, request.Minutes.Value, cancellationToken);
                    if (!upsertResult.Success)
                        return Result.Failure(new Error("IFoodMerchant.SetPreparationTimeFailed", upsertResult.ErrorMessage ?? "Failed to set preparation time on iFood."));
                }

                var mapping = await mappingRepository.GetByBranchForUpdateAsync(request.BranchId, cancellationToken);
                if (mapping is null)
                    return Result.Failure(new Error("IFoodMerchant.NoMerchantId", "This branch has no iFood Merchant ID configured."));

                var set = mapping.SetPreparationTime(request.Minutes);
                if (set.IsFailure)
                    return set;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }
}
