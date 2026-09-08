using System.Net;
using FluentAssertions;
using NSubstitute;
using SyncBar.Application.Abstractions.Integrations.Keeta;
using SyncBar.Infrastructure.Integrations.Keeta;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Keeta;

public sealed class KeetaAuthClientTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly IKeetaCredentialsResolver _credentialsResolver = Substitute.For<IKeetaCredentialsResolver>();
    private readonly KeetaAuthClient _client;

    public KeetaAuthClientTests()
    {
        _client = new KeetaAuthClient(new HttpClient(_handler), _credentialsResolver);
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new KeetaCredentials("https://open.mykeeta.com/api/open/opendelivery", "client-1", "secret-1", "app-1"));
    }

    private HttpRequestMessage LastRequest => _handler.Requests[^1];

    [Fact]
    public async Task GetAuthorizationUrlAsync_Success_ShouldBuildQueryWithEscapedParamsAndReturnUrl()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"merchantAuthorizationUrl":"https://keeta.example/authorize?token=abc"}""");

        var url = await _client.GetAuthorizationUrlAsync(1, 2, "https://syncbar.example/callback?companyId=1&branchId=2", CancellationToken.None);

        url.Should().Be("https://keeta.example/authorize?token=abc");
        LastRequest.RequestUri!.ToString().Should().Contain("clientId=client-1");
        LastRequest.RequestUri!.ToString().Should().Contain(Uri.EscapeDataString("https://syncbar.example/callback?companyId=1&branchId=2"));
        LastRequest.RequestUri!.ToString().Should().StartWith("https://open.mykeeta.com/api/open/opendelivery/oauth/authorization/url");
    }

    [Fact]
    public async Task GetAuthorizationUrlAsync_Failure_ShouldThrowWithStatusAndBody()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"message":"clientId inválido"}""");

        var act = () => _client.GetAuthorizationUrlAsync(1, 2, "https://x", CancellationToken.None);

        (await act.Should().ThrowAsync<InvalidOperationException>())
            .WithMessage("*400*clientId inválido*");
    }

    [Fact]
    public async Task GetAccessTokenAsync_Success_ShouldPostClientCredentialsAndMapResponse()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"access_token":"tok_1","token_type":"Bearer","expires_in":3600}""");

        var result = await _client.GetAccessTokenAsync(1, 2, CancellationToken.None);

        result.AccessToken.Should().Be("tok_1");
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().Be(3600);
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequest.RequestUri!.ToString().Should().EndWith("oauth/token");
        var body = _handler.RequestBodies.Last()!;
        body.Should().Contain("\"client_id\":\"client-1\"");
        body.Should().Contain("\"grant_type\":\"app_level_token\"");
        body.Should().Contain("\"client_secret\":\"secret-1\"");
    }

    [Fact]
    public async Task GetAccessTokenAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.Unauthorized, """{"message":"credenciais inválidas"}""");

        var act = () => _client.GetAccessTokenAsync(1, 2, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetMerchantInfoAsync_Success_ShouldSendBearerAuthAndDefaultPaging()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """
            {"userId":10,"brandId":20,"brandName":"Marca X","authorizedShops":[
                {"id":100,"name":"Loja Centro","address":"Rua A, 1","longitude":-46.6,"latitude":-23.5,"timeZone":"America/Sao_Paulo"}
            ]}
            """);

        var result = await _client.GetMerchantInfoAsync(1, 2, "auth-1", "bearer-tok", cancellationToken: CancellationToken.None);

        result.UserId.Should().Be(10);
        result.BrandId.Should().Be(20);
        result.BrandName.Should().Be("Marca X");
        result.AuthorizedShops.Should().ContainSingle(s => s.Id == 100 && s.Name == "Loja Centro");
        LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        LastRequest.Headers.Authorization!.Parameter.Should().Be("bearer-tok");
        LastRequest.RequestUri!.ToString().Should().Contain("oauth/authorized/auth-1/merchantInfo?pageNum=1&pageSize=10");
    }

    [Fact]
    public async Task GetMerchantInfoAsync_CustomPaging_ShouldIncludeInUrl()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"userId":1,"brandId":1,"brandName":"X","authorizedShops":[]}""");

        await _client.GetMerchantInfoAsync(1, 2, "auth-1", "tok", pageNum: 3, pageSize: 50, cancellationToken: CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().Contain("pageNum=3&pageSize=50");
    }

    [Fact]
    public async Task GetMerchantInfoAsync_NoShops_ShouldReturnEmptyList()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"userId":1,"brandId":1,"brandName":"X","authorizedShops":[]}""");

        var result = await _client.GetMerchantInfoAsync(1, 2, "auth-1", "tok", cancellationToken: CancellationToken.None);

        result.AuthorizedShops.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMerchantInfoAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.Forbidden, """{"message":"acesso negado"}""");

        var act = () => _client.GetMerchantInfoAsync(1, 2, "auth-1", "tok", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
