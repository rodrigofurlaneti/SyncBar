using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Keeta;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

public sealed class KeetaSignedRequestTests
{
    [Fact]
    public void Sign_SortsQueryAndCanonicalizesNestedJson()
    {
        var uri = new Uri("https://example.com/orders?z=3&a=hello%20world");
        var expectedInput = "https://example.com/orders&a=hello world&z=3&{\"a\":{\"b\":2,\"c\":1},\"z\":3}";
        var expected = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes(expectedInput)));
        KeetaSignedRequest.Sign(uri, "{\"z\":3, \"a\":{\"c\":1,\"b\":2}}", "secret").Should().Be(expected);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("")]
    public void Sign_EmptyBodyDoesNotAppendSeparators(string body)
    {
        var expected = Convert.ToBase64String(HMACSHA256.HashData(Encoding.UTF8.GetBytes("secret"), Encoding.UTF8.GetBytes("https://example.com/orders")));
        KeetaSignedRequest.Sign(new Uri("https://example.com/orders"), body, "secret").Should().Be(expected);
    }

    [Theory]
    [InlineData("1E30", "1e+30")]
    [InlineData("1E-6", "0.000001")]
    [InlineData("1E-7", "1e-7")]
    [InlineData("1E20", "100000000000000000000")]
    [InlineData("-0", "0")]
    [InlineData("333333333.33333329", "333333333.3333333")]
    public void Canonicalize_UsesEcmaNumberNotation(string source, string expected)
    {
        using var document = JsonDocument.Parse(source);
        KeetaSignedRequest.Canonicalize(document.RootElement).Should().Be(expected);
    }
}
