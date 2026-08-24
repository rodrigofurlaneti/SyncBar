using Microsoft.Extensions.Caching.Memory;
using SyncBar.Application.Abstractions.Integrations.IFood;
using SyncBar.Application.Abstractions.Messaging;
using SyncBar.Domain.Primitives;
using SyncBar.Domain.Repositories;

namespace SyncBar.Application.Features.Integrations.IFood.Merchant;

internal sealed class GetIFoodMerchantStatusQueryHandler(
    IBranchRepository branchRepository,
    IIFoodTokenProvider tokenProvider,
    IIFoodIntegrationSettingRepository settingRepository,
    IIFoodMerchantMappingRepository mappingRepository,
    IIFoodMerchantClient merchantClient,
    IMemoryCache cache,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork)
    : BaseQueryHandler<GetIFoodMerchantStatusQuery, IFoodMerchantStatusResponse>(logRepository, unitOfWork)
{
    // Fase 20 (2026-08-24): esse endpoint é chamado por até 3 telas do front (Dashboard e Status
    // Detalhado, ambas com refetchInterval de 30s, mais a tela de Integrações) e cada uma delas
    // usa o retry padrão do TanStack Query — sem essa cache, uma falha (ex.: 403 de permissão no
    // iFood, ver Fase 19) virava uma rajada de várias chamadas reais ao iFood em poucos segundos
    // (uma por retry, de cada tela aberta). TTL menor que os 30s do polling do front pra não
    // atrasar a detecção de uma mudança real de status — só colapsa rajadas dentro da mesma
    // janela de polling, não é uma cache "de verdade" de longa duração. Cacheia sucesso E falha
    // (mesmo padrão tolerante já usado no dedup de eventos do polling de pedidos) — uma falha de
    // permissão não vai se resolver sozinha em 25s, então não faz sentido gastar mais uma chamada
    // real só porque um retry do front bateu de novo nesse intervalo.
    private static readonly TimeSpan StatusCacheTtl = TimeSpan.FromSeconds(25);

    private static string CacheKey(long branchId) => $"ifood:merchant-status:{branchId}";

    public override async Task<Result<IFoodMerchantStatusResponse>> Handle(
        GetIFoodMerchantStatusQuery request, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(CacheKey(request.BranchId), out Result<IFoodMerchantStatusResponse>? cached) && cached is not null)
            return cached;

        var result = await ExecuteWithLogAsync(
            nameof(GetIFoodMerchantStatusQueryHandler),
            nameof(Handle),
            null,
            async (userIdBox) =>
            {
                var resolved = await IFoodMerchantResolution.ResolveAsync(
                    request.BranchId, branchRepository, tokenProvider, settingRepository, mappingRepository, cancellationToken);
                if (resolved.IsFailure)
                    return Result.Failure<IFoodMerchantStatusResponse>(resolved.Error);

                var (_, merchantId, token, _) = resolved.Value;
                var status = await merchantClient.GetStatusAsync(token, merchantId, cancellationToken);
                if (!status.Success)
                    return Result.Failure<IFoodMerchantStatusResponse>(new Error("IFoodMerchant.StatusFailed", status.ErrorMessage ?? "Failed to fetch status from iFood."));

                var validations = status.Validations
                    .Select(v => new IFoodMerchantValidationResponse(v.Id, v.State, v.Message))
                    .ToList();

                return Result.Success(new IFoodMerchantStatusResponse(status.OperationState, status.Available, validations));
            });

        cache.Set(CacheKey(request.BranchId), result, StatusCacheTtl);
        return result;
    }
}
