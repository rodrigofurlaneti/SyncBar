using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using SyncBar.Infrastructure.Integrations.Asaas;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasPaymentServiceTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly AsaasPaymentService _service;

    public AsaasPaymentServiceTests()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        var settings = new AsaasSettings { BaseUrlSandBox = "https://sandbox.asaas.com/api/v3", ApiKeySandBox = "key" };
        var authClient = new AsaasAuthClient(new HttpClient(_handler), Options.Create(settings), env);
        _service = new AsaasPaymentService(authClient);
    }

    [Fact]
    public async Task ConsultarSaldoAsync_Success_ShouldGetBalanceAndReturnRawBody()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"balance":123.45}""");

        var result = await _service.ConsultarSaldoAsync();

        result.Should().Be("""{"balance":123.45}""");
        _handler.Requests[^1].Method.Should().Be(HttpMethod.Get);
        _handler.Requests[^1].RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/finance/balance"));
    }

    [Fact]
    public async Task ConsultarSaldoAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "erro interno");

        var act = () => _service.ConsultarSaldoAsync();

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
