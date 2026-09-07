using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Keeta;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

public sealed class KeetaSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var settings = new KeetaSettings();

        settings.BaseUrl.Should().Be("https://open.mykeeta.com/api/open/opendelivery");
        settings.ClientId.Should().Be(string.Empty);
        settings.ClientSecret.Should().Be(string.Empty);
        settings.AppId.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var settings = new KeetaSettings
        {
            BaseUrl = "https://custom.keeta.example",
            ClientId = "client-1",
            ClientSecret = "secret-1",
            AppId = "app-1"
        };

        settings.BaseUrl.Should().Be("https://custom.keeta.example");
        settings.ClientId.Should().Be("client-1");
        settings.ClientSecret.Should().Be("secret-1");
        settings.AppId.Should().Be("app-1");
    }
}
