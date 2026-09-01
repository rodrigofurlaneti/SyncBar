using Microsoft.Extensions.Caching.Memory;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Ifood;

/// <summary>
/// Cache de access token OAuth2 por empresa, em memória (IMemoryCache). Fecha a lacuna deixada
/// na fase 1 (o teste de conexão pegava um token novo a cada chamada) — necessário agora porque
/// o polling de pedidos roda a cada 30s e pegar token novo toda vez não escala e arrisca rate
/// limit. TTL do cache = expiresIn da resposta do Ifood menos 60s de margem (nunca um valor
/// fixo hardcoded, conforme a doc de Autenticação).
/// </summary>
internal sealed class IfoodTokenProvider(
    IMemoryCache cache,
    IIfoodIntegrationSettingRepository settingRepository,
    ISecretProtector secretProtector,
    IIfoodAuthClient authClient) : IIfoodTokenProvider
{
    // Mesma purpose usada em SaveIfoodSettingsCommandHandler/TestIfoodConnectionCommandHandler —
    // trocar quebra a descriptografia de segredos já salvos.
    private const string ProtectorPurpose = "SyncBar.Integrations.Ifood.ClientSecret.v1";

    private static string CacheKey(long companyId) => $"Ifood:token:{companyId}";

    public async Task<string?> GetAccessTokenAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<string>(CacheKey(companyId), out var cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var setting = await settingRepository.GetByCompanyAsync(companyId, cancellationToken);
        if (setting is null || !setting.Enabled || setting.ClientId is null || setting.ClientSecretEncrypted is null)
            return null;

        string clientSecret;
        try
        {
            clientSecret = secretProtector.Unprotect(ProtectorPurpose, setting.ClientSecretEncrypted);
        }
        catch
        {
            return null; // chave de proteção mudou/foi perdida — mesmo tratamento do teste de conexão
        }

        var auth = await authClient.AuthenticateAsync(setting.ClientId, clientSecret, cancellationToken);
        if (!auth.Success || auth.AccessToken is null)
            return null;

        var ttl = TimeSpan.FromSeconds(Math.Max(30, (auth.ExpiresInSeconds ?? 180) - 60));
        cache.Set(CacheKey(companyId), auth.AccessToken, ttl);
        return auth.AccessToken;
    }

    public void Invalidate(long companyId) => cache.Remove(CacheKey(companyId));
}
