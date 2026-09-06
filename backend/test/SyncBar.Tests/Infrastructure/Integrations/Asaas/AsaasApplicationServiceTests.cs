using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;
using AppAsaas = SyncBar.Application.Abstractions.Integrations.Asaas;
using SyncBar.Infrastructure.Integrations.Asaas;
using SyncBar.Tests.Infrastructure.Integrations.TestSupport;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasApplicationServiceTests
{
    private readonly FakeHttpMessageHandler _handler = new();
    private readonly AppAsaas.IAsaasCredentialsResolver _credentialsResolver = Substitute.For<AppAsaas.IAsaasCredentialsResolver>();
    private readonly AsaasApplicationService _appService;

    public AsaasApplicationServiceTests()
    {
        var env = Substitute.For<IHostEnvironment>();
        env.EnvironmentName.Returns(Environments.Development);
        var settings = new AsaasSettings { BaseUrlSandBox = "https://sandbox.asaas.com/api/v3", ApiKeySandBox = "key" };
        var authClient = new AsaasAuthClient(new HttpClient(_handler), Options.Create(settings), env);
        var innerService = new AsaasService(authClient, _credentialsResolver);
        _appService = new AsaasApplicationService(innerService);
    }

    private static AppAsaas.CreditCardRequest AppCard() => new("João Silva", "4111111111111111", "12", "2030", "123");
    private static AppAsaas.CreditCardHolderInfoRequest AppHolder(string? phone = "11999999999") =>
        new("João Silva", "joao@example.com", "12345678900", "01310000", "100", phone);

    [Fact]
    public async Task CreatePixPaymentAsync_ShouldMapResponseFieldsAndOmitDueDate()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","customer":"cus_1","value":50,"netValue":48,"billingType":"PIX","status":"PENDING","dueDate":"2026-01-01","invoiceUrl":"https://x","bankSlipUrl":null}""");

        var result = await _appService.CreatePixPaymentAsync("cus_1", 50m, DateTime.Today, "desc", CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.Status.Should().Be("PENDING");
        result.Value.Should().Be(50m);
        result.NetValue.Should().Be(48m);
        result.InvoiceUrl.Should().Be("https://x");
        result.BankSlipUrl.Should().BeNull();
    }

    [Fact]
    public async Task GetPixQrCodeAsync_ShouldMapAllFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"encodedImage":"img","payload":"copia-cola","expirationDate":"2026-01-01"}""");

        var result = await _appService.GetPixQrCodeAsync("pay_1", CancellationToken.None);

        result.EncodedImage.Should().Be("img");
        result.Payload.Should().Be("copia-cola");
        result.ExpirationDate.Should().Be("2026-01-01");
    }

    [Fact]
    public async Task GetBoletoIdentificationFieldAsync_ShouldMapAllFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"identificationField":"341","barCode":"341999","nossoNumero":"1"}""");

        var result = await _appService.GetBoletoIdentificationFieldAsync("pay_1", CancellationToken.None);

        result.IdentificationField.Should().Be("341");
        result.BarCode.Should().Be("341999");
        result.NossoNumero.Should().Be("1");
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_ShouldMapCardAndHolderIntoInfraDtosAndReturnToken()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","status":"CONFIRMED","value":100,"netValue":97,"invoiceUrl":null,"creditCard":{"creditCardNumber":"1111","creditCardBrand":"VISA","creditCardToken":"tok_1"}}""");

        var result = await _appService.CreateCreditCardPaymentAsync(
            "cus_1", 100m, DateTime.Today, "desc", AppCard(), AppHolder(), cancellationToken: CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.CreditCardToken.Should().Be("tok_1");
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_HolderPhoneNull_ShouldMapToEmptyStringInInfraRequest()
    {
        var handler = _handler;
        handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","status":"CONFIRMED","value":100,"netValue":97,"invoiceUrl":null,"creditCard":null}""");

        await _appService.CreateCreditCardPaymentAsync(
            "cus_1", 100m, DateTime.Today, "desc", AppCard(), AppHolder(phone: null), cancellationToken: CancellationToken.None);

        var body = System.Text.Json.JsonDocument.Parse(handler.RequestBodies[^1]!).RootElement;
        body.GetProperty("creditCardHolderInfo").GetProperty("phone").GetString().Should().Be(string.Empty);
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_ShouldMapResponseFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","customer":"cus_1","value":10,"netValue":null,"billingType":"CREDIT_CARD","status":"CONFIRMED","dueDate":"2026-01-01","invoiceUrl":"https://x","bankSlipUrl":"https://y"}""");

        var result = await _appService.CreatePaymentWithCardTokenAsync("cus_1", 10m, DateTime.Today, "desc", "tok_1", cancellationToken: CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.InvoiceUrl.Should().Be("https://x");
        result.BankSlipUrl.Should().Be("https://y");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithHolderInfo_ShouldMapCardAndHolderThenReturnToken()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"creditCardToken":"tok_1","creditCardBrand":"VISA","creditCardNumber":"1111"}""");

        var result = await _appService.TokenizeCreditCardAsync("cus_1", AppCard(), AppHolder(), CancellationToken.None);

        result.CreditCardToken.Should().Be("tok_1");
        result.CreditCardBrand.Should().Be("VISA");
        result.CreditCardNumber.Should().Be("1111");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithoutHolderInfo_ShouldPassNullThroughAndOmitFromPayload()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"creditCardToken":"tok_1","creditCardBrand":"VISA","creditCardNumber":"1111"}""");

        await _appService.TokenizeCreditCardAsync("cus_1", AppCard(), null, CancellationToken.None);

        var body = System.Text.Json.JsonDocument.Parse(_handler.RequestBodies[^1]!).RootElement;
        body.TryGetProperty("creditCardHolderInfo", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CreatePaymentAsync_ShouldMapResponseFields()
    {
        _handler.EnqueueJson(HttpStatusCode.OK,
            """{"id":"pay_1","customer":"cus_1","value":10,"netValue":9.5,"billingType":"PIX","status":"PENDING","dueDate":"2026-01-01","invoiceUrl":"https://x","bankSlipUrl":null}""");

        var result = await _appService.CreatePaymentAsync("cus_1", "pix", 10m, DateTime.Today, "desc", cancellationToken: CancellationToken.None);

        result.Id.Should().Be("pay_1");
        result.NetValue.Should().Be(9.5m);
    }

    [Fact]
    public async Task ConfigureForTenantAsync_ShouldDelegateToInnerService()
    {
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api", "tenant-key"));

        await _appService.ConfigureForTenantAsync(1, 2, CancellationToken.None);

        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        await _appService.DeleteCustomerAsync("cus_1", CancellationToken.None);

        _handler.Requests[^1].Headers.GetValues("access_token").Should().ContainSingle().Which.Should().Be("tenant-key");
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldDelegateAndReturnId()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, """{"id":"cus_1","name":"João","cpfCnpj":"1","email":"j@x.com"}""");

        var id = await _appService.CreateCustomerAsync("João", "1", "j@x.com", null, CancellationToken.None);

        id.Should().Be("cus_1");
    }

    [Fact]
    public async Task DeleteCustomerAsync_ShouldDelegate()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _appService.DeleteCustomerAsync("cus_1", CancellationToken.None);

        _handler.Requests[^1].Method.Should().Be(HttpMethod.Delete);
    }

    [Fact]
    public async Task DeletePaymentAsync_ShouldDelegate()
    {
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _appService.DeletePaymentAsync("pay_1", CancellationToken.None);

        _handler.Requests[^1].RequestUri!.ToString().Should().EndWith("payments/pay_1");
    }
}
