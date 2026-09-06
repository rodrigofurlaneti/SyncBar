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

    private async Task SetupValidTenantAsync()
    {
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api/", "tenant-key"));

        await _service.ConfigureForTenantAsync(1, 2, CancellationToken.None);
    }

    private static AsaasPaymentResponse CreatePaymentResponse(string id, string status = "PENDING", decimal value = 100m, decimal? netValue = 98m)
    {
        return new AsaasPaymentResponse(
            id,
            status,
            value,
            netValue,
            "2026-12-31",
            "2026-12-31",
            "Pedido Descrição",
            "https://invoice.example/view",
            "https://slip.example/view");
    }

    [Fact]
    public async Task SendAsync_WhenNotConfigured_ShouldThrowInvalidOperationException()
    {
        var act = () => _service.DeletePaymentAsync("pay_123", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AsaasService não foi configurado*");
    }

    [Fact]
    public async Task ConfigureForTenantAsync_BaseUrlWithoutTrailingSlash_ShouldTrimAndFormatUriProperly()
    {
        _credentialsResolver.ResolveAsync(1, null, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api", "tenant-key"));

        await _service.ConfigureForTenantAsync(1, null, CancellationToken.None);

        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        await _service.DeletePaymentAsync("pay-1", CancellationToken.None);

        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/payments/pay-1"));
        LastRequest.Headers.GetValues("access_token").Should().ContainSingle().Which.Should().Be("tenant-key");
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsaasErrorAsync_WithJsonErrors_ShouldThrowFormattedException()
    {
        await SetupValidTenantAsync();

        var errorBody = JsonSerializer.Serialize(new
        {
            errors = new[]
            {
                new { code = "invalid_customer", description = "Cliente não encontrado" },
                new { code = "invalid_value", description = "Valor inválido" }
            }
        });

        _handler.EnqueueJson(HttpStatusCode.BadRequest, errorBody);

        var act = () => _service.DeletePaymentAsync("pay_invalid", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.WithMessage("*Erro Asaas (HTTP BadRequest): invalid_customer: Cliente não encontrado; invalid_value: Valor inválido*");
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsaasErrorAsync_WithEmptyJsonErrors_ShouldThrowRawContent()
    {
        await SetupValidTenantAsync();

        var errorBody = JsonSerializer.Serialize(new { errors = Array.Empty<object>() });
        _handler.EnqueueJson(HttpStatusCode.BadRequest, errorBody);

        var act = () => _service.DeletePaymentAsync("pay_invalid", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.WithMessage("*Falha na requisição Asaas (HTTP BadRequest)*");
    }

    [Fact]
    public async Task EnsureSuccessOrThrowAsaasErrorAsync_WithNonJsonError_ShouldCatchJsonExceptionAndThrowRawContent()
    {
        await SetupValidTenantAsync();

        _handler.EnqueueJson(HttpStatusCode.InternalServerError, "<html>Gateway Timeout 504</html>");

        var act = () => _service.DeletePaymentAsync("pay_invalid", CancellationToken.None);

        var ex = await act.Should().ThrowAsync<HttpRequestException>();
        ex.WithMessage("*Falha na requisição Asaas (HTTP InternalServerError): <html>Gateway Timeout 504</html>*");
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldSendPayloadAndReturnId()
    {
        await SetupValidTenantAsync();

        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(new { id = "cus_12345" }));

        var result = await _service.CreateCustomerAsync("Cliente Teste", "12345678901", "teste@teste.com", "11999998888", CancellationToken.None);

        result.Should().Be("cus_12345");
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/customers"));
        LastRequestBody.Should().Contain("Cliente Teste")
            .And.Contain("12345678901")
            .And.Contain("teste@teste.com")
            .And.Contain("11999998888");
    }

    [Fact]
    public async Task DeleteCustomerAsync_ShouldSendDeleteRequest()
    {
        await SetupValidTenantAsync();
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _service.DeleteCustomerAsync("cus_123", CancellationToken.None);

        LastRequest.Method.Should().Be(HttpMethod.Delete);
        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/customers/cus_123"));
    }

    [Fact]
    public async Task CreatePixPaymentAsync_ShouldPostCorrectPayload()
    {
        await SetupValidTenantAsync();

        var expectedResponse = CreatePaymentResponse("pay_pix_1", "PENDING", 150.00m, 148.01m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

        var result = await _service.CreatePixPaymentAsync("cus_1", 150.00m, new DateTime(2026, 10, 15), "Pedido 10", CancellationToken.None);

        result.Id.Should().Be("pay_pix_1");
        LastRequest.Method.Should().Be(HttpMethod.Post);
        LastRequestBody.Should().Contain("\"billingType\":\"PIX\"")
            .And.Contain("\"value\":150")
            .And.Contain("\"dueDate\":\"2026-10-15\"");
    }

    [Fact]
    public async Task CreatePaymentAsync_SingleInstallmentWithoutToken_ShouldPostPayload()
    {
        await SetupValidTenantAsync();

        var expectedResponse = CreatePaymentResponse("pay_bol_1", "PENDING", 200.00m, 198.00m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

        var result = await _service.CreatePaymentAsync(
            "cus_1", "BOLETO", 200.00m, new DateTime(2026, 12, 1), "Boleto", null, 1, CancellationToken.None);

        result.Id.Should().Be("pay_bol_1");
        LastRequestBody.Should().Contain("\"billingType\":\"BOLETO\"")
            .And.NotContain("creditCardToken")
            .And.NotContain("installmentCount");
    }

    [Fact]
    public async Task CreatePaymentAsync_MultipleInstallmentsAndCardToken_ShouldIncludeBranchProperties()
    {
        await SetupValidTenantAsync();

        var expectedResponse = CreatePaymentResponse("pay_cc_1", "CONFIRMED", 300.00m, 290.00m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expectedResponse));

        var result = await _service.CreatePaymentAsync(
            "cus_1", "credit_card", 300.00m, new DateTime(2026, 12, 1), "Cartao", "tok_cc_123", 3, CancellationToken.None);

        result.Id.Should().Be("pay_cc_1");
        LastRequestBody.Should().Contain("\"billingType\":\"CREDIT_CARD\"")
            .And.Contain("\"creditCardToken\":\"tok_cc_123\"")
            .And.Contain("\"installmentCount\":3")
            .And.Contain("\"totalValue\":300");
    }

    [Fact]
    public async Task GetPixQrCodeAsync_ShouldSendGetAndReturnQrCode()
    {
        await SetupValidTenantAsync();

        var expected = new AsaasPixQrCodeResponse("img_base64", "payload_copia_cola", "2026-12-31T23:59:59");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var result = await _service.GetPixQrCodeAsync("pay_123", CancellationToken.None);

        result.EncodedImage.Should().Be("img_base64");
        result.Payload.Should().Be("payload_copia_cola");
        result.ExpirationDate.Should().Be("2026-12-31T23:59:59");
        LastRequest.Method.Should().Be(HttpMethod.Get);
        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/payments/pay_123/pixQrCode"));
    }

    [Fact]
    public async Task GetBoletoIdentificationFieldAsync_ShouldSendGetAndReturnDetails()
    {
        await SetupValidTenantAsync();

        var expected = new AsaasBoletoIdentificationFieldResponse("00190.00009 01234.567890 12345.678901 1 12345678901234", "001912345678901234", "123456");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var result = await _service.GetBoletoIdentificationFieldAsync("pay_123", CancellationToken.None);

        result.IdentificationField.Should().Be(expected.IdentificationField);
        result.BarCode.Should().Be(expected.BarCode);
        LastRequest.Method.Should().Be(HttpMethod.Get);
        LastRequest.RequestUri.Should().Be(new Uri("https://tenant.asaas.example/api/payments/pay_123/identificationField"));
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_WithRemoteIpAndInstallments_ShouldIncludeFields()
    {
        await SetupValidTenantAsync();

        var cardInfo = new AsaasCreditCardInfo("1234", "MASTERCARD", "tok_123");
        var expected = new AsaasCreditCardPaymentResponse("pay_cc_1", "CONFIRMED", 500m, 480m, "https://invoice", cardInfo);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var card = new CreditCardRequest("Titular Teste", "4111111111111111", "12", "2028", "123");
        var holder = new CreditCardHolderInfoRequest("Titular Teste", "email@teste.com", "12345678909", "01001000", "100", "1199999999");

        var result = await _service.CreateCreditCardPaymentAsync(
            "cus_1", 500m, new DateTime(2026, 11, 1), "Descricao", card, holder, "127.0.0.1", 2, CancellationToken.None);

        result.Id.Should().Be("pay_cc_1");
        LastRequestBody.Should().Contain("\"remoteIp\":\"127.0.0.1\"")
            .And.Contain("\"installmentCount\":2")
            .And.Contain("\"totalValue\":500");
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_WithoutRemoteIpAndSingleInstallment_ShouldOmitFields()
    {
        await SetupValidTenantAsync();

        var expected = new AsaasCreditCardPaymentResponse("pay_cc_2", "CONFIRMED", 100m, 95m, null, null);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var card = new CreditCardRequest("Titular Teste", "4111111111111111", "12", "2028", "123");
        var holder = new CreditCardHolderInfoRequest("Titular Teste", "email@teste.com", "12345678909", "01001000", "100", "1199999999");

        var result = await _service.CreateCreditCardPaymentAsync(
            "cus_1", 100m, new DateTime(2026, 11, 1), "Descricao", card, holder, null, 1, CancellationToken.None);

        result.Id.Should().Be("pay_cc_2");
        LastRequestBody.Should().NotContain("remoteIp")
            .And.NotContain("installmentCount");
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_WithRemoteIpAndInstallments_ShouldIncludeFields()
    {
        await SetupValidTenantAsync();

        var expected = CreatePaymentResponse("pay_tok_1", "CONFIRMED", 250m, 240m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var result = await _service.CreatePaymentWithCardTokenAsync(
            "cus_1", 250m, new DateTime(2026, 11, 1), "Desc", "token_xyz", 4, "192.168.1.1", CancellationToken.None);

        result.Id.Should().Be("pay_tok_1");
        LastRequestBody.Should().Contain("\"creditCardToken\":\"token_xyz\"")
            .And.Contain("\"installmentCount\":4")
            .And.Contain("\"remoteIp\":\"192.168.1.1\"");
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_WithoutRemoteIpAndSingleInstallment_ShouldOmitFields()
    {
        await SetupValidTenantAsync();

        var expected = CreatePaymentResponse("pay_tok_2", "CONFIRMED", 50m, 48m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var result = await _service.CreatePaymentWithCardTokenAsync(
            "cus_1", 50m, new DateTime(2026, 11, 1), "Desc", "token_xyz", 1, null, CancellationToken.None);

        result.Id.Should().Be("pay_tok_2");
        LastRequestBody.Should().Contain("\"creditCardToken\":\"token_xyz\"")
            .And.NotContain("installmentCount")
            .And.NotContain("remoteIp");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithHolderInfo_ShouldPostHolderInfo()
    {
        await SetupValidTenantAsync();

        var expected = new AsaasTokenizeCreditCardResponse("card_token_999", "MASTERCARD", "1234");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var card = new CreditCardRequest("Titular", "5111111111111111", "08", "2029", "999");
        var holder = new CreditCardHolderInfoRequest("Titular", "holder@teste.com", "12345678901", "12345-000", "50", "11988887777");

        var result = await _service.TokenizeCreditCardAsync("cus_1", card, holder, CancellationToken.None);

        result.CreditCardToken.Should().Be("card_token_999");
        LastRequestBody.Should().Contain("\"creditCardHolderInfo\"")
            .And.Contain("holder@teste.com");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithoutHolderInfo_ShouldOmitHolderInfo()
    {
        await SetupValidTenantAsync();

        var expected = new AsaasTokenizeCreditCardResponse("card_token_888", "VISA", "4321");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(expected));

        var card = new CreditCardRequest("Titular", "4111111111111111", "08", "2029", "999");

        var result = await _service.TokenizeCreditCardAsync("cus_1", card, null, CancellationToken.None);

        result.CreditCardToken.Should().Be("card_token_888");
        LastRequestBody.Should().NotContain("creditCardHolderInfo");
    }
}