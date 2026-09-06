using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Repositories;
using SyncBar.Infrastructure.Integrations.Keeta;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

public sealed class KeetaCredentialsResolverTests
{
    private readonly IKeetaIntegrationSettingRepository _settingRepository = Substitute.For<IKeetaIntegrationSettingRepository>();

    private static readonly KeetaSettings AppSettings = new()
    {
        BaseUrl = "https://open.mykeeta.com/api/open/opendelivery",
        ClientId = "appsettings-client-id",
        ClientSecret = "appsettings-client-secret",
        AppId = "appsettings-app-id",
    };

    private KeetaCredentialsResolver CreateResolver() => new(_settingRepository, Options.Create(AppSettings));

    [Fact]
    public async Task ResolveAsync_NoSettingInDatabase_ShouldFallBackToAppsettings()
    {
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns((KeetaIntegrationSetting?)null);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ClientId.Should().Be("appsettings-client-id");
        result.ClientSecret.Should().Be("appsettings-client-secret");
        result.AppId.Should().Be("appsettings-app-id");
        result.BaseUrl.Should().Be("https://open.mykeeta.com/api/open/opendelivery");
    }

    [Fact]
    public async Task ResolveAsync_SettingWithFullCredentialsAndCustomBaseUrl_ShouldUseDatabaseValues()
    {
        var setting = KeetaIntegrationSetting.Create(1, 1).Value;
        setting.SaveCredentials("db-client-id", "db-client-secret", "db-app-id", "https://custom.keeta.example");
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ClientId.Should().Be("db-client-id");
        result.ClientSecret.Should().Be("db-client-secret");
        result.AppId.Should().Be("db-app-id");
        result.BaseUrl.Should().Be("https://custom.keeta.example");
    }

    [Fact]
    public async Task ResolveAsync_SettingWithCredentialsButNoCustomBaseUrl_ShouldUseAppsettingsBaseUrl()
    {
        var setting = KeetaIntegrationSetting.Create(1, 1).Value;
        setting.SaveCredentials("db-client-id", "db-client-secret", "db-app-id");
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.BaseUrl.Should().Be("https://open.mykeeta.com/api/open/opendelivery");
    }

    [Theory]
    [InlineData(null, "secret", "app")]
    [InlineData("client", null, "app")]
    [InlineData("client", "secret", null)]
    [InlineData("", "secret", "app")]
    public async Task ResolveAsync_SettingWithIncompleteCredentials_ShouldFallBackToAppsettings(
        string? clientId, string? clientSecret, string? appId)
    {
        var setting = KeetaIntegrationSetting.Create(1, 1).Value;
        setting.SaveCredentials(clientId!, clientSecret!, appId!);
        _settingRepository.GetByBranchOrCompanyFallbackAsync(1, 1, Arg.Any<CancellationToken>()).Returns(setting);

        var result = await CreateResolver().ResolveAsync(1, 1, CancellationToken.None);

        result.ClientId.Should().Be("appsettings-client-id");
    }

    [Fact]
    public async Task ResolveDefaultAsync_ShouldAlwaysReturnAppsettingsCredentials()
    {
        var result = await CreateResolver().ResolveDefaultAsync(CancellationToken.None);

        result.ClientId.Should().Be("appsettings-client-id");
        result.ClientSecret.Should().Be("appsettings-client-secret");
        result.AppId.Should().Be("appsettings-app-id");
        result.BaseUrl.Should().Be("https://open.mykeeta.com/api/open/opendelivery");
        await _settingRepository.DidNotReceive().GetByBranchOrCompanyFallbackAsync(Arg.Any<long>(), Arg.Any<long?>(), Arg.Any<CancellationToken>());
    }
}
