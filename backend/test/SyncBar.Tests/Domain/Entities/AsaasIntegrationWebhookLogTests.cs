using FluentAssertions;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Enums;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AsaasIntegrationWebhookLogTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var result = AsaasIntegrationWebhookLog.Create(
                1, 2, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{\"event\":\"PAYMENT_RECEIVED\"}", "{}", "127.0.0.1");

            result.IsSuccess.Should().BeTrue();
            var log = result.Value;
            log.CompanyId.Should().Be(1);
            log.BranchId.Should().Be(2);
            log.Event.Should().Be("PAYMENT_RECEIVED");
            log.AsaasEventId.Should().Be("evt-1");
            log.PaymentId.Should().Be("pay_1");
            log.Payload.Should().Be("{\"event\":\"PAYMENT_RECEIVED\"}");
            log.RequestHeaders.Should().Be("{}");
            log.IpAddress.Should().Be("127.0.0.1");
            log.Status.Should().Be(WebhookLogStatus.Pending);
            log.IsActive.Should().BeTrue();
            log.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            log.ProcessedAt.Should().BeNull();
            log.UpdatedAt.Should().BeNull();
            log.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void Create_WithoutBranchIdOrOptionalFields_ShouldAllowNulls()
        {
            var result = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_CREATED", null, null, "{}");

            result.IsSuccess.Should().BeTrue();
            result.Value.BranchId.Should().BeNull();
            result.Value.AsaasEventId.Should().BeNull();
            result.Value.PaymentId.Should().BeNull();
            result.Value.RequestHeaders.Should().BeNull();
            result.Value.IpAddress.Should().BeNull();
        }

        [Fact]
        public void Create_WithInvalidCompanyId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationWebhookLog.Create(0, null, "PAYMENT_CREATED", null, null, "{}");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CompanyId.Invalid");
        }

        [Fact]
        public void Create_WithInvalidBranchId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationWebhookLog.Create(1, 0, "PAYMENT_CREATED", null, null, "{}");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("BranchId.Invalid");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyEvent_ShouldReturnFailure(string? eventName)
        {
            var result = AsaasIntegrationWebhookLog.Create(1, null, eventName!, null, null, "{}");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Event.Empty");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyPayload_ShouldReturnFailure(string? payload)
        {
            var result = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_CREATED", null, null, payload!);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("Payload.Empty");
        }

        [Fact]
        public void MarkAsProcessed_WhenPending_ShouldSucceedAndSetStatusAndProcessedAt()
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

            var result = log.MarkAsProcessed();

            result.IsSuccess.Should().BeTrue();
            log.Status.Should().Be(WebhookLogStatus.Processed);
            log.ErrorMessage.Should().BeNull();
            log.ProcessedAt.Should().NotBeNull();
            log.ProcessedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            log.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsProcessed_WhenAlreadyProcessed_ShouldReturnFailure()
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
            log.MarkAsProcessed();

            var result = log.MarkAsProcessed();

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("WebhookLog.AlreadyProcessed");
        }

        [Fact]
        public void MarkAsProcessed_AfterHavingFailed_ShouldSucceedAndClearErrorMessage()
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;
            log.MarkAsFailed("erro anterior");

            var result = log.MarkAsProcessed();

            result.IsSuccess.Should().BeTrue();
            log.Status.Should().Be(WebhookLogStatus.Processed);
            log.ErrorMessage.Should().BeNull();
        }

        [Fact]
        public void MarkAsFailed_WithErrorMessage_ShouldSucceedAndSetStatusAndErrorMessage()
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

            var result = log.MarkAsFailed("pedido nao encontrado");

            result.IsSuccess.Should().BeTrue();
            log.Status.Should().Be(WebhookLogStatus.Failed);
            log.ErrorMessage.Should().Be("pedido nao encontrado");
            log.ProcessedAt.Should().NotBeNull();
            log.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void MarkAsFailed_WithEmptyErrorMessage_ShouldReturnFailure(string? errorMessage)
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

            var result = log.MarkAsFailed(errorMessage!);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ErrorMessage.Empty");
            log.Status.Should().Be(WebhookLogStatus.Pending);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            var log = AsaasIntegrationWebhookLog.Create(1, null, "PAYMENT_RECEIVED", "evt-1", "pay_1", "{}").Value;

            log.Deactivate();

            log.IsActive.Should().BeFalse();
            log.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(AsaasIntegrationWebhookLog), true) as AsaasIntegrationWebhookLog;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
