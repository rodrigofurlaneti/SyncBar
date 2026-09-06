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

    private async Task SetupValidTenantAsync()
    {
        _credentialsResolver.ResolveAsync(1, 2, Arg.Any<CancellationToken>())
            .Returns(new AppAsaas.AsaasCredentials("https://tenant.asaas.example/api/", "tenant-key"));

        await _appService.ConfigureForTenantAsync(1, 2, CancellationToken.None);
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
            "Descricao",
            "https://invoice",
            "https://slip");
    }

    [Fact]
    public async Task ConfigureForTenantAsync_ShouldDelegateToInnerService()
    {
        await SetupValidTenantAsync();

        _handler.EnqueueJson(HttpStatusCode.OK, "{}");
        await _appService.DeleteCustomerAsync("cus_1", CancellationToken.None);

        _handler.Requests[^1].Headers.GetValues("access_token").Should().ContainSingle().Which.Should().Be("tenant-key");
    }

    [Fact]
    public async Task CreateCustomerAsync_ShouldDelegateAndReturnId()
    {
        await SetupValidTenantAsync();
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(new { id = "cus_app_1" }));

        var result = await _appService.CreateCustomerAsync("Nome", "123", "email@mail.com", "999", CancellationToken.None);

        result.Should().Be("cus_app_1");
    }

    [Fact]
    public async Task DeletePaymentAsync_ShouldDelegateSuccessfully()
    {
        await SetupValidTenantAsync();
        _handler.EnqueueJson(HttpStatusCode.OK, "{}");

        await _appService.DeletePaymentAsync("pay_del_1", CancellationToken.None);

        _handler.Requests[^1].Method.Should().Be(HttpMethod.Delete);
        _handler.Requests[^1].RequestUri!.AbsolutePath.Should().EndWith("payments/pay_del_1");
    }
    
    [Fact]
    public async Task GetPixQrCodeAsync_ShouldMapToApplicationResponse()
    {
        await SetupValidTenantAsync();
        const string expDate = "2026-12-31T23:59:59";
        var infraResponse = new AsaasPixQrCodeResponse("img_base64", "payload_copia", expDate);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var result = await _appService.GetPixQrCodeAsync("pay_1", CancellationToken.None);

        result.EncodedImage.Should().Be("img_base64");
        result.Payload.Should().Be("payload_copia");
        result.ExpirationDate.Should().Be(expDate);
    }

    [Fact]
    public async Task GetBoletoIdentificationFieldAsync_ShouldMapToApplicationResponse()
    {
        await SetupValidTenantAsync();
        var infraResponse = new AsaasBoletoIdentificationFieldResponse("field_123", "barcode_123", "nosso_123");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var result = await _appService.GetBoletoIdentificationFieldAsync("pay_1", CancellationToken.None);

        result.IdentificationField.Should().Be("field_123");
        result.BarCode.Should().Be("barcode_123");
        result.NossoNumero.Should().Be("nosso_123");
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_ShouldMapEntitiesAndReturnAppResponse()
    {
        await SetupValidTenantAsync();
        var cardInfo = new AsaasCreditCardInfo("1234", "MASTERCARD", "card_tok_1");
        var infraResponse = new AsaasCreditCardPaymentResponse("pay_cc_1", "CONFIRMED", 200m, 190m, "https://inv", cardInfo);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var card = new AppAsaas.CreditCardRequest("Holder", "1234567812345678", "11", "2027", "123");
        var holder = new AppAsaas.CreditCardHolderInfoRequest("Holder", "h@mail.com", "12345678901", "12345000", "10", "1199999999");

        var result = await _appService.CreateCreditCardPaymentAsync("cus_1", 200m, DateTime.UtcNow, "CC", card, holder, "10.0.0.1", 1, CancellationToken.None);

        result.Id.Should().Be("pay_cc_1");
        result.CreditCardToken.Should().Be("card_tok_1");
    }

    [Fact]
    public async Task CreateCreditCardPaymentAsync_WithNullPhone_ShouldFallbackToEmptyString()
    {
        await SetupValidTenantAsync();
        var infraResponse = new AsaasCreditCardPaymentResponse("pay_cc_2", "CONFIRMED", 150m, 140m, null, null);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var card = new AppAsaas.CreditCardRequest("Holder", "1234567812345678", "11", "2027", "123");
        var holder = new AppAsaas.CreditCardHolderInfoRequest("Holder", "h@mail.com", "12345678901", "12345000", "10", null);

        var result = await _appService.CreateCreditCardPaymentAsync("cus_1", 150m, DateTime.UtcNow, "CC", card, holder, null, 1, CancellationToken.None);

        result.Id.Should().Be("pay_cc_2");
        result.CreditCardToken.Should().BeNull();
    }

    [Fact]
    public async Task CreatePaymentWithCardTokenAsync_ShouldMapToApplicationResponse()
    {
        await SetupValidTenantAsync();
        var infraResponse = CreatePaymentResponse("pay_tok_1", "CONFIRMED", 120m, 115m);
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var result = await _appService.CreatePaymentWithCardTokenAsync("cus_1", 120m, DateTime.UtcNow, "Tokenized", "tok_xyz", 2, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be("pay_tok_1");
        result.Value.Should().Be(120m);
        result.InvoiceUrl.Should().Be("https://invoice");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithHolderInfoAndNullPhone_ShouldMapSuccessfully()
    {
        await SetupValidTenantAsync();
        var infraResponse = new AsaasTokenizeCreditCardResponse("token_ok", "VISA", "9999");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var card = new AppAsaas.CreditCardRequest("Holder", "4111111111111111", "05", "2030", "555");
        var holder = new AppAsaas.CreditCardHolderInfoRequest("Holder", "h@mail.com", "12345678901", "12345000", "10", null);

        var result = await _appService.TokenizeCreditCardAsync("cus_1", card, holder, CancellationToken.None);

        result.CreditCardToken.Should().Be("token_ok");
        result.CreditCardBrand.Should().Be("VISA");
        result.CreditCardNumber.Should().Be("9999");
    }

    [Fact]
    public async Task TokenizeCreditCardAsync_WithNullHolderInfo_ShouldPassNullHolder()
    {
        await SetupValidTenantAsync();
        var infraResponse = new AsaasTokenizeCreditCardResponse("token_null_holder", "MASTERCARD", "8888");
        _handler.EnqueueJson(HttpStatusCode.OK, JsonSerializer.Serialize(infraResponse));

        var card = new AppAsaas.CreditCardRequest("Holder", "5111111111111111", "05", "2030", "555");

        var result = await _appService.TokenizeCreditCardAsync("cus_1", card, null, CancellationToken.None);

        result.CreditCardToken.Should().Be("token_null_holder");
    }
}