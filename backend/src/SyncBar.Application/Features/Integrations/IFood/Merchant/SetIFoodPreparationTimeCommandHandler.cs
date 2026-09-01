using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.Ifood.Merchant;

// Único fluxo do módulo Merchant que exige o header X-Ifood-Customer-ID (ver comentário em
// IIfoodMerchantClient) — sem IfoodIntegrationSetting.IfoodCustomerId configurado, retorna erro
// amigável em vez de tentar a chamada (que falharia no Ifood de qualquer forma).
internal sealed class SetIfoodPreparationTimeCommandHandler(
    IBranchRepository branchRepository,
    IIfoodTokenProvider tokenProvider,
    IIfoodIntegrationSettingRepository settingRepository,
    IIfoodMerchantMappingRepository mappingRepository,
    IIfoodMerchantClient merchantClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseCommandHandler<SetIfoodPreparationTimeCommand>(logRepository, unitOfWork)
{
    // Campo explícito: capturar o parâmetro primário que também vai para a base dispara CS9107.
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public override async Task<Result> Handle(SetIfoodPreparationTimeCommand request, CancellationToken cancellationToken)
    {
        return await ExecuteWithLogAsync(
            nameof(SetIfoodPreparationTimeCommandHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var context = await ResolveMerchantContextAsync(request.BranchId, cancellationToken);
                if (context.IsFailure)
                    return Result.Failure(context.Error);

                var (merchantId, token, IfoodCustomerId) = context.Value;

                var syncResult = await SyncPreparationTimeOnIfoodAsync(
                    token, merchantId, IfoodCustomerId, request.Minutes, cancellationToken);
                if (syncResult.IsFailure)
                    return syncResult;

                var mappingResult = await UpdateMappingPreparationTimeAsync(request.BranchId, request.Minutes, cancellationToken);
                if (mappingResult.IsFailure)
                    return mappingResult;

                await _unitOfWork.CommitAsync(cancellationToken);
                return Result.Success();
            });
    }

    private async Task<Result<(string MerchantId, string Token, string IfoodCustomerId)>> ResolveMerchantContextAsync(
        long branchId, CancellationToken cancellationToken)
    {
        var resolved = await IfoodMerchantResolution.ResolveAsync(
            branchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
        if (resolved.IsFailure)
            return Result.Failure<(string MerchantId, string Token, string IfoodCustomerId)>(resolved.Error);

        var (_, merchantId, token, IfoodCustomerId) = resolved.Value;
        if (string.IsNullOrWhiteSpace(IfoodCustomerId))
            return Result.Failure<(string MerchantId, string Token, string IfoodCustomerId)>(new Error(
                "IfoodMerchant.MissingCustomerId",
                "Configure o Ifood Customer ID nas credenciais do app antes de definir o tempo de preparo."));

        (string MerchantId, string Token, string IfoodCustomerId) value = (merchantId, token, IfoodCustomerId);
        return Result.Success(value);
    }

    private async Task<Result> SyncPreparationTimeOnIfoodAsync(
        string token, string merchantId, string IfoodCustomerId, int? minutes, CancellationToken cancellationToken)
    {
        if (minutes is null)
        {
            var deleteResult = await merchantClient.DeletePreparationTimeAsync(token, merchantId, IfoodCustomerId, cancellationToken);
            return deleteResult.Success
                ? Result.Success()
                : Result.Failure(new Error("IfoodMerchant.DeletePreparationTimeFailed", deleteResult.ErrorMessage ?? "Failed to reset preparation time on Ifood."));
        }

        var upsertResult = await merchantClient.UpsertPreparationTimeAsync(token, merchantId, IfoodCustomerId, minutes.Value, cancellationToken);
        return upsertResult.Success
            ? Result.Success()
            : Result.Failure(new Error("IfoodMerchant.SetPreparationTimeFailed", upsertResult.ErrorMessage ?? "Failed to set preparation time on Ifood."));
    }

    private async Task<Result> UpdateMappingPreparationTimeAsync(long branchId, int? minutes, CancellationToken cancellationToken)
    {
        var mapping = await mappingRepository.GetByBranchForUpdateAsync(branchId, cancellationToken);
        if (mapping is null)
            return Result.Failure(new Error("IfoodMerchant.NoMerchantId", "This branch has no Ifood Merchant ID configured."));

        return mapping.SetPreparationTime(minutes);
    }
}
