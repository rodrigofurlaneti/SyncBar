using FluentAssertions;
using SyncBar.Infrastructure.Integrations.Asaas;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Integrations.Asaas;

public sealed class AsaasPaymentDataResponseTests
{
    [Fact]
    public void Construction_ShouldExposeProvidedValues()
    {
        var response = new AsaasPaymentDataResponse(
            "pay_1", "cus_1", "CONFIRMED", 150.50m, 145.00m, "PIX", "https://invoice", "https://boleto");

        response.Id.Should().Be("pay_1");
        response.Customer.Should().Be("cus_1");
        response.Status.Should().Be("CONFIRMED");
        response.Value.Should().Be(150.50m);
        response.NetValue.Should().Be(145.00m);
        response.BillingType.Should().Be("PIX");
        response.InvoiceUrl.Should().Be("https://invoice");
        response.BankSlipUrl.Should().Be("https://boleto");
    }

    [Fact]
    public void Construction_WithNullableFieldsNull_ShouldAllowNulls()
    {
        var response = new AsaasPaymentDataResponse(
            "pay_2", "cus_2", "PENDING", 100m, null, "BOLETO", null, null);

        response.NetValue.Should().BeNull();
        response.InvoiceUrl.Should().BeNull();
        response.BankSlipUrl.Should().BeNull();
    }

    [Fact]
    public void Equality_WithSameValues_ShouldBeEqual()
    {
        var first = new AsaasPaymentDataResponse("pay_1", "cus_1", "CONFIRMED", 150.50m, 145.00m, "PIX", "https://invoice", "https://boleto");
        var second = new AsaasPaymentDataResponse("pay_1", "cus_1", "CONFIRMED", 150.50m, 145.00m, "PIX", "https://invoice", "https://boleto");

        first.Should().Be(second);
        (first == second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        var first = new AsaasPaymentDataResponse("pay_1", "cus_1", "CONFIRMED", 150.50m, 145.00m, "PIX", "https://invoice", "https://boleto");
        var second = new AsaasPaymentDataResponse("pay_1", "cus_1", "CONFIRMED", 999.99m, 145.00m, "PIX", "https://invoice", "https://boleto");

        first.Should().NotBe(second);
        (first == second).Should().BeFalse();
    }
}
