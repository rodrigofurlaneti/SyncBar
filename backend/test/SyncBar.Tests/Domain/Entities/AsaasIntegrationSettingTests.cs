using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AsaasIntegrationSettingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            var result = AsaasIntegrationSetting.Create(1, 2, "api-key", "webhook-secret", "Production", "wallet-1", false);

            result.IsSuccess.Should().BeTrue();
            var setting = result.Value;
            setting.CompanyId.Should().Be(1);
            setting.BranchId.Should().Be(2);
            setting.ApiKeyEncrypted.Should().Be("api-key");
            setting.WebhookSecretEncrypted.Should().Be("webhook-secret");
            setting.Environment.Should().Be("Production");
            setting.WalletId.Should().Be("wallet-1");
            setting.IsActive.Should().BeFalse();
            setting.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            setting.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithoutBranchId_ShouldRepresentCompanyWideScope()
        {
            var result = AsaasIntegrationSetting.Create(1, null, "api-key");

            result.IsSuccess.Should().BeTrue();
            result.Value.BranchId.Should().BeNull();
        }

        [Fact]
        public void Create_WithDefaultArguments_ShouldDefaultToSandboxAndActive()
        {
            var result = AsaasIntegrationSetting.Create(1, null, "api-key");

            result.IsSuccess.Should().BeTrue();
            result.Value.Environment.Should().Be("Sandbox");
            result.Value.IsActive.Should().BeTrue();
            result.Value.WebhookSecretEncrypted.Should().BeNull();
            result.Value.WalletId.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankEnvironment_ShouldDefaultToSandbox(string? environment)
        {
            var result = AsaasIntegrationSetting.Create(1, null, "api-key", environment: environment!);

            result.IsSuccess.Should().BeTrue();
            result.Value.Environment.Should().Be("Sandbox");
        }

        [Fact]
        public void Create_WithInvalidCompanyId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationSetting.Create(0, null, "api-key");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("CompanyId.Invalid");
        }

        [Fact]
        public void Create_WithInvalidBranchId_ShouldReturnFailure()
        {
            var result = AsaasIntegrationSetting.Create(1, 0, "api-key");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("BranchId.Invalid");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyApiKey_ShouldReturnFailure(string? apiKey)
        {
            var result = AsaasIntegrationSetting.Create(1, null, apiKey!);

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ApiKey.Empty");
        }

        [Fact]
        public void UpdateDetails_WithNewApiKey_ShouldUpdateItAndUpdatedAt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "old-key").Value;

            var result = setting.UpdateDetails(apiKeyEncrypted: "new-key");

            result.IsSuccess.Should().BeTrue();
            setting.ApiKeyEncrypted.Should().Be("new-key");
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void UpdateDetails_WithEmptyApiKey_ShouldReturnFailureAndNotChangeIt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "old-key").Value;

            var result = setting.UpdateDetails(apiKeyEncrypted: "   ");

            result.IsFailure.Should().BeTrue();
            result.Error.Code.Should().Be("ApiKey.Empty");
            setting.ApiKeyEncrypted.Should().Be("old-key");
        }

        [Fact]
        public void UpdateDetails_WithNullApiKey_ShouldKeepExistingValue()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "old-key").Value;

            var result = setting.UpdateDetails();

            result.IsSuccess.Should().BeTrue();
            setting.ApiKeyEncrypted.Should().Be("old-key");
        }

        [Fact]
        public void UpdateDetails_WithNewWebhookSecret_ShouldUpdateIt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;

            setting.UpdateDetails(webhookSecretEncrypted: "new-secret");

            setting.WebhookSecretEncrypted.Should().Be("new-secret");
        }

        [Fact]
        public void UpdateDetails_WithBlankEnvironment_ShouldKeepExistingEnvironment()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key", environment: "Production").Value;

            setting.UpdateDetails(environment: "   ");

            setting.Environment.Should().Be("Production");
        }

        [Fact]
        public void UpdateDetails_WithNewEnvironment_ShouldUpdateIt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key", environment: "Sandbox").Value;

            setting.UpdateDetails(environment: "Production");

            setting.Environment.Should().Be("Production");
        }

        [Fact]
        public void UpdateDetails_WithNewWalletId_ShouldUpdateIt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;

            setting.UpdateDetails(walletId: "wallet-2");

            setting.WalletId.Should().Be("wallet-2");
        }

        [Fact]
        public void UpdateDetails_WithIsActiveProvided_ShouldUpdateIt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key", isActive: true).Value;

            setting.UpdateDetails(isActive: false);

            setting.IsActive.Should().BeFalse();
        }

        [Fact]
        public void UpdateDetails_WithoutIsActive_ShouldKeepExistingValue()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key", isActive: true).Value;

            setting.UpdateDetails();

            setting.IsActive.Should().BeTrue();
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key").Value;

            setting.Deactivate();

            setting.IsActive.Should().BeFalse();
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void Activate_ShouldSetIsActiveTrueAndUpdatedAt()
        {
            var setting = AsaasIntegrationSetting.Create(1, null, "api-key", isActive: false).Value;

            setting.Activate();

            setting.IsActive.Should().BeTrue();
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            var instance = Activator.CreateInstance(typeof(AsaasIntegrationSetting), true) as AsaasIntegrationSetting;

            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
