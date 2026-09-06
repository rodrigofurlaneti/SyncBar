using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using AppAsaas = SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Infrastructure.Integrations.Asaas;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasServiceTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly AppAsaas.IAsaasCredentialsResolver _credentialsResolver = Substitute.For<AppAsaas.IAsaasCredentialsResolver>();
    private readonly AsaasService _service;

    public AsaasServiceTests()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        var settings = new AsaasSettings
        {
            BaseUrlSandBox = "https://sandbox.asaas.com/api/v3",
            ApiKeySandBox = "initial-key",
        };
        var httpClient = new HttpClient(_handler);
        var authClient = new AsaasAuthClient(httpClient, Options.Create(settings), env);
        _service = new AsaasService(authClient, _credentialsResolver);
    }

    private string LastRequestBody => _handler.RequestBodies[^1]!;
    private HttpRequestMessage LastRequest => _handler.Requests[^1];

    [Fact]
    public async Task ConfigureForTenantAsync_ShouldResolveCredentialsAndReconfigureClient()
    {
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api", "tenant-key"));

        await _service.ConfigureForTenantAsync(1, 2, CancellationToken.None);

        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        await _service.DeletePaymentAsync("pay-1", CancellationToken.None);

        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/payments/pay-1"));
        LastRequest.Headers.GetValues("access_token").Should().ContainSingle().Which.Should().Be("tenant-key");
    }

    [Fact]
    public async Task ConfigureForTenantAsync_BaseUrlWithoutTrailingSlash_ShouldAppendSlash()
    {
        _credentialsResolver.ResolveAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api", "tenant-key"));

        await _service.ConfigureForTenantAsync(1, null, CancellationToken.None);

        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        await _service.DeletePaymentAsync("pay-1", CancellationToken.None);

        LastRequest.RequestUri!.ToString().Should().StartWith("https://tenant.asaas.example/api/");
    }

    [Fact]
    public async Task CreateCustomerAsync_Success_ShouldPostPayloadAndReturnId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cus_123","name":"João","cpfCnpj":"12345678900","email":"joao@example.com"}""");

        var id = await _service.CreateCustomerAsync("João", "12345678900", "joao@example.com", "11999999999", CancellationToken.None);

        id.Should().Be("cus_123");
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/customers"));
        LastRequest.Method.Should().Be(HttpMethod.Post);
        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("name").GetString().Should().Be("João");
        body.GetProperty("cpfCnpj").GetString().Should().Be("12345678900");
        body.GetProperty("mobilePhone").GetString().Should().Be("11999999999");
    }

    [Fact]
    public async Task CreateCustomerAsync_StructuredAsaasError_ShouldThrowWithJoinedMessages()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest,
            """{"errors":[{"code":"invalid_cpfCnpj","description":"CPF inválido"},{"code":"invalid_email","description":"E-mail inválido"}]}""");

        var act = () => _service.CreateCustomerAsync("João", "000", "x", null, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>())
            .WithMessage("*invalid_cpfCnpj: CPF inválido*invalid_email: E-mail inválido*");
    }

    [Fact]
    public async Task CreateCustomerAsync_MalformedErrorBody_ShouldThrowWithRawContent()
    {
        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "not-json-at-all");

        var act = () => _service.CreateCustomerAsync("João", "000", "x", null, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("*not-json-at-all*");
    }

    [Fact]
    public async Task CreateCustomerAsync_ErrorBodyWithEmptyErrorsList_ShouldThrowWithRawContent()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[]}""");

        var act = () => _service.CreateCustomerAsync("João", "000", "x", null, CancellationToken.None);

        (await act.Should().ThrowAsync<HttpRequestException>()).WithMessage("*errors*");
    }

    [Fact]
    public async Task DeleteCustomerAsync_Success_ShouldSendDeleteToCorrectUrl()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _service.DeleteCustomerAsync("cus_123", CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Delete);
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/customers/cus_123"));
    }

    [Fact]
    public async Task DeleteCustomerAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, """{"errors":[{"code":"not_found","description":"Cliente não encontrado"}]}""");

        var act = () => _service.DeleteCustomerAsync("cus_missing", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreatePixPaymentAsync_Success_ShouldPostPixPayloadAndReturnPayment()
    {
        var dueDate = new DateTime(2026, 1, 15);
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","customer":"cus_1","value":50.0,"netValue":48.5,"billingType":"PIX","status":"PENDING","dueDate":"2026-01-15"}""");

        var result = await _service.CreatePixPaymentAsync("cus_1", 50m, dueDate, "Pedido 42", CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.Status.Should().Be("PENDING");
        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("billingType").GetString().Should().Be("PIX");
        body.GetProperty("dueDate").GetString().Should().Be("2026-01-15");
    }

    [Fact]
    public async Task CreatePixPaymentAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[{"code":"invalid_customer","description":"Cliente inválido"}]}""");

        var act = () => _service.CreatePixPaymentAsync("cus_bad", 10m, DateTime.Today, "x", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreatePaymentAsync_DefaultBillingTypeCasing_ShouldBeUppercasedInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"boleto","status":"PENDING","dueDate":"2026-01-01"}""");

        await _service.CreatePaymentAsync("cus_1", "boleto", 10m, DateTime.Today, "desc", cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("billingType").GetString().Should().Be("BOLETO");
        body.TryGetProperty("creditCardToken", out _).Should().BeFalse();
        body.TryGetProperty("installmentCount", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreatePaymentAsync_WithCreditCardToken_ShouldIncludeTokenInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"CREDIT_CARD","status":"CONFIRMED","dueDate":"2026-01-01"}""");

        await _service.CreatePaymentAsync("cus_1", "credit_card", 10m, DateTime.Today, "desc", "token-abc", cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("creditCardToken").GetString().Should().Be("token-abc");
    }

    [Fact]
    public async Task CreatePaymentAsync_WithMultipleInstallments_ShouldIncludeInstallmentCountAndTotalValue()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"CREDIT_CARD","status":"CONFIRMED","dueDate":"2026-01-01"}""");

        await _service.CreatePaymentAsync("cus_1", "credit_card", 10m, DateTime.Today, "desc", installmentCount: 3, cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("installmentCount").GetInt32().Should().Be(3);
        body.GetProperty("totalValue").GetDecimal().Should().Be(10m);
    }

    [Fact]
    public async Task CreatePaymentAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[{"code":"invalid_value","description":"Valor inválido"}]}""");

        var act = () => _service.CreatePaymentAsync("cus_1", "PIX", -1m, DateTime.Today, "desc", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetPixQrCodeAsync_Success_ShouldGetAndReturnQrCode()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"encodedImage":"base64img","payload":"copia-e-cola","expirationDate":"2026-01-01"}""");

        var result = await _service.GetPixQrCodeAsync("pay_1", CancellationToken.None);

        result.EncodedImage.Should().Be("base64img");
        LastRequest.Method.Should().Be(HttpMethod.Get);
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/payments/pay_1/pixQrCode"));
    }

    [Fact]
    public async Task GetPixQrCodeAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, """{"errors":[{"code":"not_found","description":"Pagamento não encontrado"}]}""");

        var act = () => _service.GetPixQrCodeAsync("pay_missing", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task GetBoletoIdentificationFieldAsync_Success_ShouldGetAndReturnField()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"identificationField":"34191...","barCode":"34191790010104350","nossoNumero":"123"}""");

        var result = await _service.GetBoletoIdentificationFieldAsync("pay_1", CancellationToken.None);

        result.IdentificationField.Should().Be("34191...");
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/payments/pay_1/identificationField"));
    }

    [Fact]
    public async Task GetBoletoIdentificationFieldAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, """{"errors":[{"code":"not_found","description":"Boleto não encontrado"}]}""");

        var act = () => _service.GetBoletoIdentificationFieldAsync("pay_missing", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    private static CreditCardRequest ValidCard() => new("João Silva", "4111111111111111", "12", "2030", "123");
    private static CreditCardHolderInfoRequest ValidHolder() => new("João Silva", "joao@example.com", "12345678900", "01310000", "100", "11999999999");

    [Fact]
    public async Task CreateCreditCardPaymentAsync_Success_ShouldPostFullPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","status":"CONFIRMED","value":100,"netValue":97,"invoiceUrl":"https://x","creditCard":{"creditCardNumber":"1111","creditCardBrand":"VISA","creditCardToken":"tok_1"}}""");

        var result = await _service.CreateCreditCardPaymentAsync(
            "cus_1", 100m, DateTime.Today, "desc", ValidCard(), ValidHolder(), cancellationToken: CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.CreditCard!.CreditCardToken.Should().Be("tok_1");
        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("billingType").GetString().Should().Be("CREDIT_CARD");
        body.GetProperty("creditCard").GetProperty("number").GetString().Should().Be("4111111111111111");
        body.TryGetProperty("remoteIp", out _).Should().BeFalse();
        body.TryGetProperty("installmentCount", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_WithRemoteIpAndInstallments_ShouldIncludeBothInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","status":"CONFIRMED","value":100,"netValue":97,"invoiceUrl":null,"creditCard":null}""");

        await _service.CreateCreditCardPaymentAsync(
            "cus_1", 100m, DateTime.Today, "desc", ValidCard(), ValidHolder(), remoteIp: "1.2.3.4", installmentCount: 2, cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("remoteIp").GetString().Should().Be("1.2.3.4");
        body.GetProperty("installmentCount").GetInt32().Should().Be(2);
        body.GetProperty("totalValue").GetDecimal().Should().Be(100m);
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[{"code":"invalid_card","description":"Cartão inválido"}]}""");

        var act = () => _service.CreateCreditCardPaymentAsync("cus_1", 10m, DateTime.Today, "desc", ValidCard(), ValidHolder(), cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_Default_ShouldNotIncludeRemoteIpOrInstallments()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"CREDIT_CARD","status":"CONFIRMED","dueDate":"2026-01-01"}""");

        await _service.CreatePaymentWithCardTokenAsync("cus_1", 10m, DateTime.Today, "desc", "tok_1", cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("creditCardToken").GetString().Should().Be("tok_1");
        body.TryGetProperty("remoteIp", out _).Should().BeFalse();
        body.TryGetProperty("installmentCount", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_WithRemoteIpAndInstallments_ShouldIncludeBoth()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"CREDIT_CARD","status":"CONFIRMED","dueDate":"2026-01-01"}""");

        await _service.CreatePaymentWithCardTokenAsync("cus_1", 10m, DateTime.Today, "desc", "tok_1", installmentCount: 4, remoteIp: "9.9.9.9", cancellationToken: CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.GetProperty("remoteIp").GetString().Should().Be("9.9.9.9");
        body.GetProperty("installmentCount").GetInt32().Should().Be(4);
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[{"code":"invalid_token","description":"Token inválido"}]}""");

        var act = () => _service.CreatePaymentWithCardTokenAsync("cus_1", 10m, DateTime.Today, "desc", "tok_bad", cancellationToken: CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithHolderInfo_ShouldIncludeItInPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"creditCardToken":"tok_1","creditCardBrand":"VISA","creditCardNumber":"1111"}""");

        var result = await _service.TokenizeCreditCardAsync("cus_1", ValidCard(), ValidHolder(), CancellationToken.None);

        result.CreditCardToken.Should().Be("tok_1");
        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.TryGetProperty("creditCardHolderInfo", out _).Should().BeTrue();
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/creditCard/tokenizeCreditCard"));
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithoutHolderInfo_ShouldOmitItFromPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"creditCardToken":"tok_1","creditCardBrand":"VISA","creditCardNumber":"1111"}""");

        await _service.TokenizeCreditCardAsync("cus_1", ValidCard(), null, CancellationToken.None);

        var body = JsonDocument.Parse(LastRequestBody).RootElement;
        body.TryGetProperty("creditCardHolderInfo", out _).Should().BeFalse();
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.BadRequest, """{"errors":[{"code":"invalid_card","description":"Cartão inválido"}]}""");

        var act = () => _service.TokenizeCreditCardAsync("cus_1", ValidCard(), null, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    [Fact]
    public async Task DeletePaymentAsync_Success_ShouldSendDeleteToCorrectUrl()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _service.DeletePaymentAsync("pay_1", CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Delete);
        LastRequest.RequestUri.Should().Be(new Uri("https://sandbox.asaas.com/api/v3/payments/pay_1"));
    }

    [Fact]
    public async Task DeletePaymentAsync_Failure_ShouldThrow()
    {
        _handler.EnqueueJson(HttpStatusCode.NotFound, """{"errors":[{"code":"not_found","description":"Pagamento não encontrado"}]}""");

        var act = () => _service.DeletePaymentAsync("pay_missing", CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
    }
}
