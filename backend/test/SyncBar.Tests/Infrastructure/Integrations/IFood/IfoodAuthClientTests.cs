using System.Net;
using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Ifood;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.IFood;

public sealed class IfoodAuthClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IfoodAuthClient _client;

    public IfoodAuthClientTests()
    {
        _client = new IfoodAuthClient(new HttpClient(_handler));
    }

    [Fact]
    public async Task AuthenticateAsync_Success_ShouldPostFormEncodedCredentialsAndReturnToken()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"accessToken":"tok_1","expiresIn":3600,"tokenType":"Bearer"}""");

        var result = await _client.AuthenticateAsync("client-1", "secret-1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.AccessToken.Should().Be("tok_1");
        result.ExpiresInSeconds.Should().Be(3600);
        var request = _handler.Requests[^1];
        request.Method.Should().Be(HttpMethod.Post);
        request.RequestUri.Should().Be(new Uri("https://merchant-api.Ifood.com.br/authentication/v1.0/oauth/token"));
        var body = _handler.RequestBodies[^1];
        body.Should().Contain("grantType=client_credentials");
        body.Should().Contain("clientId=client-1");
        body.Should().Contain("clientSecret=secret-1");
    }

    [Fact]
    public async Task AuthenticateAsync_HttpFailure_ShouldReturnFailureWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.Unauthorized, """{"message":"invalid client"}""");

        var result = await _client.AuthenticateAsync("bad-client", "bad-secret", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.AccessToken.Should().BeNull();
        result.ErrorMessage.Should().Contain("401").And.Contain("invalid client");
    }

    [Fact]
    public async Task AuthenticateAsync_HttpFailureWithLongBody_ShouldTruncateErrorMessage()
    {
        var longBody = new string('x', 500);
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, longBody);

        var result = await _client.AuthenticateAsync("client", "secret", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("…");
        result.ErrorMessage!.Length.Should().BeLessThan(longBody.Length);
    }

    [Fact]
    public async Task AuthenticateAsync_ResponseWithoutAccessToken_ShouldReturnFailure()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"expiresIn":3600}""");

        var result = await _client.AuthenticateAsync("client", "secret", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("accessToken");
    }

    [Fact]
    public async Task AuthenticateAsync_NetworkException_ShouldReturnFailureWithExceptionMessage()
    {
        _handler.Enqueue(_ => throw new HttpRequestException("conexão recusada"));

        var result = await _client.AuthenticateAsync("client", "secret", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("conexão recusada");
    }
}
