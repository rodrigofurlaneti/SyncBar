using Microsoft.Extensions.Options;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Keeta;

internal sealed class KeetaCredentialsResolver(
    IKeetaIntegrationSettingRepository settingRepository,
    IOptions<KeetaSettings> options) : IKeetaCredentialsResolver
{
    private readonly KeetaSettings _settings = options.Value;

    public async Task<KeetaCredentials> ResolveAsync(long companyId, long branchId, CancellationToken cancellationToken = default)
    {
        var setting = await settingRepository.GetByBranchOrCompanyFallbackAsync(companyId, branchId, cancellationToken);

        if (setting is not null
            && !string.IsNullOrWhiteSpace(setting.ClientId)
            && !string.IsNullOrWhiteSpace(setting.ClientSecret)
            && !string.IsNullOrWhiteSpace(setting.AppId))
        {
            var baseUrl = string.IsNullOrWhiteSpace(setting.BaseUrl) ? _settings.BaseUrl : setting.BaseUrl;
            return new KeetaCredentials(baseUrl, setting.ClientId, setting.ClientSecret, setting.AppId);
        }

        // Nenhuma configuração ativa no banco para esta empresa/filial — usa o appsettings
        // (credencial única de "Software Service" compartilhada por todas as empresas).
        return new KeetaCredentials(_settings.BaseUrl, _settings.ClientId, _settings.ClientSecret, _settings.AppId);
    }

    public Task<KeetaCredentials> ResolveDefaultAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(new KeetaCredentials(_settings.BaseUrl, _settings.ClientId, _settings.ClientSecret, _settings.AppId));
}
