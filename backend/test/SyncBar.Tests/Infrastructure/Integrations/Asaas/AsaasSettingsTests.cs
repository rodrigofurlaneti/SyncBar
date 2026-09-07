using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldAllBeEmptyString()
    {
        var settings = new AsaasSettings();

        settings.BaseUrl.Should().Be(string.Empty);
        settings.BaseUrlSandBox.Should().Be(string.Empty);
        settings.ApiKey.Should().Be(string.Empty);
        settings.ApiKeySandBox.Should().Be(string.Empty);
        settings.WebhookKey.Should().Be(string.Empty);
        settings.WebhookKeySandBox.Should().Be(string.Empty);
        settings.WebhookUrl.Should().Be(string.Empty);
        settings.WebhookUrlSandBox.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var settings = new AsaasSettings
        {
            BaseUrl = "https://api.asaas.com",
            BaseUrlSandBox = "https://sandbox.asaas.com",
            ApiKey = "key",
            ApiKeySandBox = "sandbox-key",
            WebhookKey = "webhook-key",
            WebhookKeySandBox = "webhook-sandbox-key",
            WebhookUrl = "https://webhook.example",
            WebhookUrlSandBox = "https://webhook-sandbox.example"
        };

        settings.BaseUrl.Should().Be("https://api.asaas.com");
        settings.BaseUrlSandBox.Should().Be("https://sandbox.asaas.com");
        settings.ApiKey.Should().Be("key");
        settings.ApiKeySandBox.Should().Be("sandbox-key");
        settings.WebhookKey.Should().Be("webhook-key");
        settings.WebhookKeySandBox.Should().Be("webhook-sandbox-key");
        settings.WebhookUrl.Should().Be("https://webhook.example");
        settings.WebhookUrlSandBox.Should().Be("https://webhook-sandbox.example");
    }
}
