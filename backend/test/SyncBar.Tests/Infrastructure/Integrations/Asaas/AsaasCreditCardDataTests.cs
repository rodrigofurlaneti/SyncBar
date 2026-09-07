using System.Text.Json;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasCreditCardDataTests
{
    [Fact]
    public void Construction_ShouldExposeProvidedValues()
    {
        var data = new AsaasCreditCardData("1234567812345678", "MASTERCARD", "tok_1");

        data.CreditCardNumber.Should().Be("1234567812345678");
        data.CreditCardBrand.Should().Be("MASTERCARD");
        data.CreditCardToken.Should().Be("tok_1");
    }

    [Fact]
    public void Construction_WithNullToken_ShouldAllowNullToken()
    {
        var data = new AsaasCreditCardData("1234567812345678", "VISA", null);

        data.CreditCardToken.Should().BeNull();
    }

    [Fact]
    public void Serialize_ShouldUseAttributedPropertyNames()
    {
        var data = new AsaasCreditCardData("1234567812345678", "MASTERCARD", "tok_1");

        var json = JsonSerializer.Serialize(data);

        json.Should().Contain("\"creditCardNumber\":\"1234567812345678\"");
        json.Should().Contain("\"creditCardBrand\":\"MASTERCARD\"");
        json.Should().Contain("\"creditCardToken\":\"tok_1\"");
    }

    [Fact]
    public void Deserialize_ShouldMapFromAttributedPropertyNames()
    {
        const string json = """{"creditCardNumber":"9999888877776666","creditCardBrand":"ELO","creditCardToken":"tok_2"}""";

        var data = JsonSerializer.Deserialize<AsaasCreditCardData>(json);

        data.Should().NotBeNull();
        data!.CreditCardNumber.Should().Be("9999888877776666");
        data.CreditCardBrand.Should().Be("ELO");
        data.CreditCardToken.Should().Be("tok_2");
    }

    [Fact]
    public void RoundTrip_WithNullToken_ShouldPreserveNull()
    {
        var original = new AsaasCreditCardData("1111222233334444", "VISA", null);

        var json = JsonSerializer.Serialize(original);
        var roundTripped = JsonSerializer.Deserialize<AsaasCreditCardData>(json);

        roundTripped.Should().Be(original);
    }
}
