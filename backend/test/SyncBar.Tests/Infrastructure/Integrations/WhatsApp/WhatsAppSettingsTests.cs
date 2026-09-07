using FluentAssertions;
using SyncBar.Infrastructure.Integrations.WhatsApp;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.WhatsApp;

public sealed class WhatsAppSettingsTests
{
    [Fact]
    public void DefaultValues_ShouldMatchExpectedDefaults()
    {
        var settings = new WhatsAppSettings();

        settings.BaseUrl.Should().Be("https://zap.sistemapocket.com.br/rest-api/");
        settings.Token.Should().Be(string.Empty);
        settings.ConnectionId.Should().Be(string.Empty);
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var settings = new WhatsAppSettings
        {
            BaseUrl = "https://custom.whatsapp.example",
            Token = "token-1",
            ConnectionId = "connection-1"
        };

        settings.BaseUrl.Should().Be("https://custom.whatsapp.example");
        settings.Token.Should().Be("token-1");
        settings.ConnectionId.Should().Be("connection-1");
    }
}
