using System.Text.Json;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasErrorDetailTests
{
    [Fact]
    public void Construction_ShouldExposeProvidedValues()
    {
        var detail = new AsaasErrorDetail("invalid_creditCard", "Cartão de crédito inválido");

        detail.Code.Should().Be("invalid_creditCard");
        detail.Description.Should().Be("Cartão de crédito inválido");
    }

    [Fact]
    public void Serialize_ShouldUseAttributedPropertyNames()
    {
        var detail = new AsaasErrorDetail("invalid_creditCard", "Invalid credit card");

        var json = JsonSerializer.Serialize(detail);

        json.Should().Contain("\"code\":\"invalid_creditCard\"");
        json.Should().Contain("\"description\":\"Invalid credit card\"");
    }

    [Fact]
    public void Deserialize_ShouldMapFromAttributedPropertyNames()
    {
        const string json = """{"code":"invalid_object","description":"Invalid object"}""";

        var detail = JsonSerializer.Deserialize<AsaasErrorDetail>(json);

        detail.Should().NotBeNull();
        detail!.Code.Should().Be("invalid_object");
        detail.Description.Should().Be("Invalid object");
    }

    [Fact]
    public void RoundTrip_ShouldPreserveEquality()
    {
        var original = new AsaasErrorDetail("bad_request", "Invalid request");

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<AsaasErrorDetail>(json);

        roundTripped.Should().Be(original);
    }
}
