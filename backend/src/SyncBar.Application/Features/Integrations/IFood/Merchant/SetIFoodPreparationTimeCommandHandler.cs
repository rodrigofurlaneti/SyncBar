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
                var context = await ResolveMerchantContextAsync(request.BranchId, cancellationToken);
                if (context.IsFailure)
                    return Result.Failure(context.Error);

                var (merchantId, token, ifoodCustomerId) = context.Value;

                var syncResult = await SyncPreparationTimeOnIFoodAsync(
                    token, merchantId, ifoodCustomerId, request.Minutes, cancellationToken);
                if (syncResult.IsFailure)
                    return syncResult;

                var mappingResult = await UpdateMappingPreparationTimeAsync(request.BranchId, request.Minutes, cancellationToken);
                if (mappingResult.IsFailure)
                    return mappingResult;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    private async Task<Result<(string MerchantId, string Token, string IFoodCustomerId)>> ResolveMerchantContextAsync(
        long branchId, CancellationToken cancellationToken)
    {
        var resolved = await IFoodMerchantResolution.ResolveAsync(
            branchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<(string MerchantId, string Token, string IFoodCustomerId)>(resolved.Error);

        var (_, merchantId, token, ifoodCustomerId) = resolved.Value;
        if (string.IsNullOrWhiteSpace(ifoodCustomerId))
            return Result.Failure<(string MerchantId, string Token, string IFoodCustomerId)>(new Error(
                "IFoodMerchant.MissingCustomerId",
                "Configure o iFood Customer ID nas credenciais do app antes de definir o tempo de preparo."));

        (string MerchantId, string Token, string IFoodCustomerId) value = (merchantId, token, ifoodCustomerId);
        return Result.Success(value);
    }

    private async Task<Result> SyncPreparationTimeOnIFoodAsync(
        string token, string merchantId, string ifoodCustomerId, int? minutes, CancellationToken cancellationToken)
    {
        if (minutes is null)
        {
            var deleteResult = await merchantClient.DeletePreparationTimeAsync(token, merchantId, ifoodCustomerId, cancellationToken);
            return deleteResult.Success
                ? Result.Success()
                : Result.Failure(new Error("IFoodMerchant.DeletePreparationTimeFailed", deleteResult.ErrorMessage ?? "Failed to reset preparation time on iFood."));
        }

        var upsertResult = await merchantClient.UpsertPreparationTimeAsync(token, merchantId, ifoodCustomerId, minutes.Value, cancellationToken);
        return upsertResult.Success
            ? Result.Success()
            : Result.Failure(new Error("IFoodMerchant.SetPreparationTimeFailed", upsertResult.ErrorMessage ?? "Failed to set preparation time on iFood."));
    }

    private async Task<Result> UpdateMappingPreparationTimeAsync(long branchId, int? minutes, CancellationToken cancellationToken)
    {
        var mapping = await mappingRepository.GetByBranchForUpdateAsync(branchId, cancellationToken);
        if (mapping is null)
            return Result.Failure(new Error("IFoodMerchant.NoMerchantId", "This branch has no iFood Merchant ID configured."));

        return mapping.SetPreparationTime(minutes);
    }
}
