using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasAuthClientTests
{
    private readonly IHostEnvironment _env = Substitute.For<IHostEnvironment>();

    private static AsaasSettings ValidSettings() => new()
    {
        BaseUrl = "https://www.asaas.com/api/v3",
        BaseUrlSandBox = "https://sandbox.asaas.com/api/v3",
        ApiKey = "prod-key",
        ApiKeySandBox = "sandbox-key",
        WebhookKey = "prod-webhook-key",
        WebhookKeySandBox = "sandbox-webhook-key",
    };

    private AsaasAuthClient CreateClient(AsaasSettings settings, HttpClient? httpClient = null)
        => new(httpClient ?? new HttpClient(), Options.Create(settings), _env);

    [Fact]
    public void Constructor_SandboxEnvironment_ShouldConfigureBaseAddressAndSandboxKey()
    {
        _env.EnvironmentName.Returns(Environments.Development);

        var client = CreateClient(ValidSettings());

        client.Client.BaseAddress.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/"));
        client.Client.DefaultRequestHeaders.GetValues("access_token").Should().ContainSingle().Which.Should().Be("sandbox-key");
    }

    [Fact]
    public void Constructor_ProductionEnvironment_ShouldConfigureBaseAddressAndProductionKey()
    {
        _env.EnvironmentName.Returns(Environments.Production);

        var client = CreateClient(ValidSettings());

        client.Client.BaseAddress.Should().Be(new Uri("https://www.asaas.com/api/v3/"));
        client.Client.DefaultRequestHeaders.GetValues("access_token").Should().ContainSingle().Which.Should().Be("prod-key");
    }

    [Fact]
    public void Constructor_BaseUrlWithoutTrailingSlash_ShouldAppendSlash()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        var settings = ValidSettings();
        settings.BaseUrlSandBox = "https://sandbox.asaas.com/api/v3";

        var client = CreateClient(settings);

        client.Client.BaseAddress!.ToString().Should().EndWith("/");
    }

    [Fact]
    public void Constructor_BaseUrlWithTrailingSlash_ShouldNotDuplicateSlash()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        var settings = ValidSettings();
        settings.BaseUrlSandBox = "https://sandbox.asaas.com/api/v3/";

        var client = CreateClient(settings);

        client.Client.BaseAddress.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/"));
    }

    [Fact]
    public void Constructor_MissingBaseUrlForEnvironment_ShouldThrow()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        var settings = ValidSettings();
        settings.BaseUrlSandBox = "";

        var act = () => CreateClient(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Sandbox*");
    }

    [Fact]
    public void Constructor_MissingApiKeyForEnvironment_ShouldThrow()
    {
        _env.EnvironmentName.Returns(Environments.Production);
        var settings = ValidSettings();
        settings.ApiKey = "  ";

        var act = () => CreateClient(settings);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Produção*");
    }

    [Fact]
    public void Constructor_AcceptHeaderNotYetSet_ShouldAddJsonAcceptHeader()
    {
        _env.EnvironmentName.Returns(Environments.Development);

        var client = CreateClient(ValidSettings());

        client.Client.DefaultRequestHeaders.Accept.Should().ContainSingle(h => h.MediaType == "application/json");
    }

    [Fact]
    public void Constructor_UserAgentNotYetSet_ShouldAddSyncBarUserAgent()
    {
        _env.EnvironmentName.Returns(Environments.Development);

        var client = CreateClient(ValidSettings());

        client.Client.DefaultRequestHeaders.UserAgent.Should().ContainSingle(u => u.Product!.Name == "SyncBar");
    }

    [Fact]
    public void Constructor_HeadersAlreadyPresentOnHttpClient_ShouldNotDuplicateThem()
    {
        _env.EnvironmentName.Returns(Environments.Development);
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("access_token", "pre-existing-token");

        var client = CreateClient(ValidSettings(), httpClient);

        client.Client.DefaultRequestHeaders.GetValues("access_token").Should().ContainSingle().Which.Should().Be("pre-existing-token");
    }

    [Fact]
    public void GetWebhookKey_SandboxEnvironment_ShouldReturnSandboxWebhookKey()
    {
        _env.EnvironmentName.Returns(Environments.Development);

        var client = CreateClient(ValidSettings());

        client.GetWebhookKey().Should().Be("sandbox-webhook-key");
    }

    [Fact]
    public void GetWebhookKey_ProductionEnvironment_ShouldReturnProductionWebhookKey()
    {
        _env.EnvironmentName.Returns(Environments.Production);

        var client = CreateClient(ValidSettings());

        client.GetWebhookKey().Should().Be("prod-webhook-key");
    }
}
