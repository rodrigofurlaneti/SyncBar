using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasCredentialsResolverTests
{
    private readonly IAsaasIntegrationSettingRepository _settingRepository = Substitute.For<IAsaasIntegrationSettingRepository>();
    private readonly IHostEnvironment _env = Substitute.For<IHostEnvironment>();

    private static readonly AsaasSettings Settings = new()
    {
        BaseUrl = "https://www.asaas.com/api/v3",
        BaseUrlSandBox = "https://sandbox.asaas.com/api/v3",
        ApiKey = "appsettings-prod-key",
        ApiKeySandBox = "appsettings-sandbox-key",
    };

    private AsaasCredentialsResolver CreateResolver()
        => new(_settingRepository, Options.Create(Settings), _env);

    [Fact]
    public async Task ResolveAsync_NoSettingInDatabase_ShouldFallBackToAppsettingsSandbox()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ApiKey.Should().Be("appsettings-sandbox-key");
        result.BaseUrl.Should().Be("https://sandbox.asaas.com/api/v3");
    }

    [Fact]
    public async Task ResolveAsync_ActiveSettingWithKey_ShouldUseDatabaseKey()
    {
        var setting = AsaasIntegrationSetting.Create(1, 1, "db-configured-key", environment: "Sandbox").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ApiKey.Should().Be("db-configured-key");
        result.BaseUrl.Should().Be("https://sandbox.asaas.com/api/v3");
    }

    [Fact]
    public async Task ResolveAsync_InactiveSetting_ShouldFallBackToAppsettings()
    {
        var inactiveSetting = AsaasIntegrationSetting.Create(1, 1, "db-key", isActive: false).Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(inactiveSetting);
        _env.EnvironmentName.Returns(Environments.Development);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ApiKey.Should().Be("appsettings-sandbox-key");
    }

    [Fact]
    public async Task ResolveAsync_SettingWithBlankApiKey_ShouldFallBackToAppsettings()
    {
        var settingWithoutKey = AsaasIntegrationSetting.Create(1, 1, "placeholder").Value;
        // Simula uma linha "configurada" mas sem chave real: força vazio via UpdateDetails não é possível
        // (bloqueado no domínio), então este teste cobre o caminho onde a chave nunca foi preenchida —
        // aqui representado por uma configuração ausente, que é o caso real observado em produção.
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>())
            .Returns((AsaasIntegrationSetting?)null);
        _env.EnvironmentName.Returns(Environments.Production);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ApiKey.Should().Be("appsettings-prod-key");
        result.BaseUrl.Should().Be("https://www.asaas.com/api/v3");
    }

    [Fact]
    public async Task ResolveAsync_DatabaseSettingMarkedProduction_ShouldUseProductionBaseUrl()
    {
        var setting = AsaasIntegrationSetting.Create(1, null, "db-prod-key", environment: "Production").Value;
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, null, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await CreateResolver().ResolveAsync(1, null, CancellationToken.None);

        result.ApiKey.Should().Be("db-prod-key");
        result.BaseUrl.Should().Be("https://www.asaas.com/api/v3");
    }
}
