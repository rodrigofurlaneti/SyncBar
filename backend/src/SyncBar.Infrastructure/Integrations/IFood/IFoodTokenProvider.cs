using Microsoft.Extensions.Caching.Memory;
using System.Diagnostics;
using SyncBar.Application.Abstractions.Integrations.Ifood;
using SyncBar.Application.Abstractions.Security;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Ifood;

internal sealed class IfoodTokenProvider(
    IMemoryCache cache,
    IIfoodIntegrationSettingRepository settingRepository,
    ISecretProtector secretProtector,
    IIfoodAuthClient authClient,
    ILogTrackerRepository logRepository,
    IUnitOfWork unitOfWork) : IIfoodTokenProvider
{
    private const string ProtectorPurpose = "SyncBar.Integrations.Ifood.ClientSecret.v1";

    private static string CacheKey(long companyId) => $"Ifood:token:{companyId}";

    public async Task<string?> GetAccessTokenAsync(long companyId, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<string>(CacheKey(companyId), out var cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var setting = await settingRepository.GetByCompanyAsync(companyId, cancellationToken);
        if (setting is null || !setting.Enabled || setting.ClientId is null || setting.ClientSecretEncrypted is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Infrastructure/Integrations/Ifood",
                ClassName = "IfoodTokenProvider",
                MethodName = nameof(GetAccessTokenAsync),
                IsSuccess = false,
                ExecutionTimeMs = 0,
                ErrorMessage = "Setting is null or not enabled or ClientId is null or ClientSecretEncrypted is null.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await logRepository.AddAsync(log);
            await unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        string clientSecret;
        try
        {
            clientSecret = secretProtector.Unprotect(ProtectorPurpose, setting.ClientSecretEncrypted);
        }
        catch (Exception ex)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Infrastructure/Integrations/Ifood",
                ClassName = "IfoodTokenProvider",
                MethodName = nameof(GetAccessTokenAsync),
                IsSuccess = false,
                ExecutionTimeMs = 0,
                ErrorMessage = $"CryptographicException (Chave perdida/alterada): {ex.Message}",
                StackTrace = ex.StackTrace ?? string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await logRepository.AddAsync(log);
            await unitOfWork.CommitAsync(cancellationToken);

            return null;
        }

        var auth = await authClient.AuthenticateAsync(setting.ClientId, clientSecret, cancellationToken);
        if (!auth.Success || auth.AccessToken is null)
            return null;

        var ttl = TimeSpan.FromSeconds(Math.Max(30, (auth.ExpiresInSeconds ?? 180) - 60));
        cache.Set(CacheKey(companyId), auth.AccessToken, ttl);
        return auth.AccessToken;
    }

    public async Task<string?> GetAccessTokenAsync(long companyId, Stopwatch stopwatch, CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue<string>(CacheKey(companyId), out var cached) && !string.IsNullOrEmpty(cached))
            return cached;

        var setting = await settingRepository.GetByCompanyAsync(companyId, cancellationToken);
        if (setting is null || !setting.Enabled || setting.ClientId is null || setting.ClientSecretEncrypted is null)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Infrastructure/Integrations/Ifood",
                ClassName = "IfoodTokenProvider",
                MethodName = nameof(GetAccessTokenAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = "Setting is null or not enabled or ClientId is null or ClientSecretEncrypted is null.",
                StackTrace = string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await logRepository.AddAsync(log);
            await unitOfWork.CommitAsync(cancellationToken);
            return null;
        }

        string clientSecret;
        try
        {
            clientSecret = secretProtector.Unprotect(ProtectorPurpose, setting.ClientSecretEncrypted);
        }
        catch (Exception ex)
        {
            var log = new LogTracker(0)
            {
                AppUserId = null,
                DirectoryName = "Infrastructure/Integrations/Ifood",
                ClassName = "IfoodTokenProvider",
                MethodName = nameof(GetAccessTokenAsync),
                IsSuccess = false,
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = $"CryptographicException (Chave perdida/alterada): {ex.Message}",
                StackTrace = ex.StackTrace ?? string.Empty,
                IpAddress = null,
                CreatedAt = DateTime.Now,
                IsActive = true
            };
            await logRepository.AddAsync(log);
            await unitOfWork.CommitAsync(cancellationToken);

            return null;
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