using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Domain.Repositories;

namespace SyncBar.Infrastructure.Integrations.Asaas;

internal sealed class AsaasCredentialsResolver(
    IAsaasIntegrationSettingRepository settingRepository,
    IOptions<AsaasSettings> options,
    IHostEnvironment env) : IAsaasCredentialsResolver
{
    private readonly AsaasSettings _settings = options.Value;

    public async Task<AsaasCredentials> ResolveAsync(long companyId, long? branchId, CancellationToken cancellationToken = default)
    {
        var setting = await settingRepository.GetByBranchOrCompanyFallbackAsync(companyId, branchId, cancellationToken);

        if (setting is not null && setting.IsActive && !string.IsNullOrWhiteSpace(setting.ApiKeyEncrypted))
        {
            var isProductionSetting = string.Equals(setting.Environment, "Production", StringComparison.OrdinalIgnoreCase);
            var settingBaseUrl = isProductionSetting ? _settings.BaseUrl : _settings.BaseUrlSandBox;
            return new AsaasCredentials(settingBaseUrl, setting.ApiKeyEncrypted);
        }

        // Nenhuma configuração ativa no banco para esta empresa/filial — usa o appsettings.
        var isProduction = env.IsProduction();
        var baseUrl = isProduction ? _settings.BaseUrl : _settings.BaseUrlSandBox;
        var apiKey = isProduction ? _settings.ApiKey : _settings.ApiKeySandBox;
        return new AsaasCredentials(baseUrl, apiKey);
    }
}
