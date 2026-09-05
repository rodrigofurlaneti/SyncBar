using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AsaasIntegrationPaymentTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var dueDate = new DateTime(2026, 9, 20);

            var result = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "pix", 100m, dueDate, 3);

            result.IsSuccess.Should().BeTrue();
            var payment = result.Value;
            payment.BranchId.Should().Be(1);
            payment.CustomerOrderId.Should().Be(10);
            payment.CustomerId.Should().Be(5);
            payment.AsaasPaymentId.Should().Be("pay_1");
            payment.BillingType.Should().Be("pix");
            payment.Value.Should().Be(100m);
            payment.DueDate.Should().Be(dueDate);
            payment.InstallmentCount.Should().Be(3);
            payment.Status.Should().Be("PENDING");
            payment.IsActive.Should().BeTrue();
            payment.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            payment.UpdatedAt.Should().BeNull();
            payment.NetValue.Should().BeNull();
        }

        [Fact]
        public void Create_WithoutCustomerId_ShouldAllowNullCustomer()
        {
            var result = AsaasIntegrationPayment.Create(1, 10, null, "pay_1", "PIX", 100m, DateTime.UtcNow);

            result.IsSuccess.Should().BeTrue();
            result.Value.CustomerId.Should().BeNull();
        }

        [Fact]
        public void Create_WithDefaultInstallmentCount_ShouldDefaultToOne()
        {
            var result = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow);

            result.IsSuccess.Should().BeTrue();
            result.Value.InstallmentCount.Should().Be(1);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithNonPositiveInstallmentCount_ShouldDefaultToOne(int installmentCount)
        {
            var result = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow, installmentCount);

            result.IsSuccess.Should().BeTrue();
            result.Value.InstallmentCount.Should().Be(1);
        }

        [Fact]
        public void Create_WithInvalidBranchId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationPayment.Create(0, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("BranchId.Invalid");
        }

        [Fact]
        public void Create_WithInvalidCustomerOrderId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationPayment.Create(1, 0, 5, "pay_1", "PIX", 100m, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CustomerOrderId.Invalid");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyAsaasPaymentId_ShouldReturnFailure(string? asaasPaymentId)
        {
            var result = AsaasIntegrationPayment.Create(1, 10, 5, asaasPaymentId!, "PIX", 100m, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("AsaasPaymentId.Empty");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Create_WithNonPositiveValue_ShouldReturnFailure(decimal value)
        {
            var result = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", value, DateTime.UtcNow);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Value.Invalid");
        }

        [Fact]
        public void SetPixDetails_ShouldSetQrCodeAndPayloadAndUpdatedAt()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.SetPixDetails("base64image", "copia-e-cola");

            payment.PixQrCodeBase64.Should().Be("base64image");
            payment.PixPayload.Should().Be("copia-e-cola");
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetUrls_ShouldSetInvoiceAndBankSlipUrlsAndUpdatedAt()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "BOLETO", 100m, DateTime.UtcNow).Value;

            payment.SetUrls("https://invoice", "https://bankslip");

            payment.InvoiceUrl.Should().Be("https://invoice");
            payment.BankSlipUrl.Should().Be("https://bankslip");
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetUrls_WithNullValues_ShouldSetNulls()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "BOLETO", 100m, DateTime.UtcNow).Value;
            payment.SetUrls("https://invoice", "https://bankslip");

            payment.SetUrls(null, null);

            payment.InvoiceUrl.Should().BeNull();
            payment.BankSlipUrl.Should().BeNull();
        }

        [Fact]
        public void SetCreditCardToken_ShouldSetTokenAndUpdatedAt()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "CREDIT_CARD", 100m, DateTime.UtcNow).Value;

            payment.SetCreditCardToken("card-token-1");

            payment.CreditCardToken.Should().Be("card-token-1");
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsPaid_WithoutExplicitPaymentDate_ShouldUseUtcNow()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.MarkAsPaid(98m);

            payment.Status.Should().Be("RECEIVED");
            payment.NetValue.Should().Be(98m);
            payment.PaymentDate.Should().NotBeNull();
            payment.PaymentDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsPaid_WithExplicitPaymentDate_ShouldUseIt()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;
            var paymentDate = new DateTime(2026, 9, 1);

            payment.MarkAsPaid(98m, paymentDate);

            payment.PaymentDate.Should().Be(paymentDate);
        }

        [Fact]
        public void MarkAsPaid_WithNullNetValue_ShouldAllowNull()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.MarkAsPaid(null);

            payment.NetValue.Should().BeNull();
        }

        [Fact]
        public void Update_WithAllFieldsProvided_ShouldUpdateAllOfThem()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;
            var paymentDate = new DateTime(2026, 9, 1);

            payment.Update("RECEIVED", 95m, paymentDate, "base64", "copia-e-cola", "https://invoice", "https://bankslip");

            payment.Status.Should().Be("RECEIVED");
            payment.NetValue.Should().Be(95m);
            payment.PaymentDate.Should().Be(paymentDate);
            payment.PixQrCodeBase64.Should().Be("base64");
            payment.PixPayload.Should().Be("copia-e-cola");
            payment.InvoiceUrl.Should().Be("https://invoice");
            payment.BankSlipUrl.Should().Be("https://bankslip");
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Update_WithBlankStatus_ShouldKeepExistingStatus(string? status)
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.Update(status!);

            payment.Status.Should().Be("PENDING");
        }

        [Fact]
        public void Update_WithoutOptionalFields_ShouldKeepExistingValuesForThem()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;
            payment.SetPixDetails("base64", "copia-e-cola");

            payment.Update("PENDING");

            payment.NetValue.Should().BeNull();
            payment.PaymentDate.Should().BeNull();
            payment.PixQrCodeBase64.Should().Be("base64");
            payment.PixPayload.Should().Be("copia-e-cola");
        }

        [Fact]
        public void UpdateStatus_WithAllFields_ShouldUpdateThem()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;
            var paymentDate = new DateTime(2026, 9, 1);

            payment.UpdateStatus("CONFIRMED", 97m, paymentDate);

            payment.Status.Should().Be("CONFIRMED");
            payment.NetValue.Should().Be(97m);
            payment.PaymentDate.Should().Be(paymentDate);
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateStatus_WithBlankStatus_ShouldKeepExistingStatus(string? status)
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.UpdateStatus(status!);

            payment.Status.Should().Be("PENDING");
        }

        [Fact]
        public void UpdateStatus_WithoutOptionalFields_ShouldKeepExistingNetValueAndPaymentDate()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.UpdateStatus("OVERDUE");

            payment.NetValue.Should().BeNull();
            payment.PaymentDate.Should().BeNull();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            var payment = AsaasIntegrationPayment.Create(1, 10, 5, "pay_1", "PIX", 100m, DateTime.UtcNow).Value;

            payment.Deactivate();

            payment.IsActive.Should().BeFalse();
            payment.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(AsaasIntegrationPayment), true) as AsaasIntegrationPayment;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
