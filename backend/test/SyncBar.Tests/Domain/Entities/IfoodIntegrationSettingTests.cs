using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // IfoodIntegrationSetting.Create has no validation branches in the source (it always returns
    // success) — no Create failure tests exist here on purpose.
    public class IfoodIntegrationSettingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;

            // Act
            var result = IfoodIntegrationSetting.Create(companyId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var setting = result.Value;
            setting.Should().NotBeNull();
            setting.CompanyId.Should().Be(companyId);
            setting.Enabled.Should().BeFalse();
            setting.ClientId.Should().BeNull();
            setting.ClientSecretEncrypted.Should().BeNull();
            setting.IfoodCustomerId.Should().BeNull();
            setting.LastConnectionTestAt.Should().BeNull();
            setting.LastConnectionTestSucceeded.Should().BeNull();
            setting.IsActive.Should().BeTrue();
            setting.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            setting.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void SaveCredentials_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var setting = IfoodIntegrationSetting.Create(1).Value;

            // Act
            var result = setting.SaveCredentials("client-123", "encrypted-secret", true, "customer-456");

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.ClientId.Should().Be("client-123");
            setting.ClientSecretEncrypted.Should().Be("encrypted-secret");
            setting.Enabled.Should().BeTrue();
            setting.IfoodCustomerId.Should().Be("customer-456");
            setting.UpdatedAt.Should().NotBeNull();
            setting.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void SaveCredentials_WithBlankSecret_ShouldKeepPreviouslySavedSecret(string? blankSecret)
        {
            // Arrange
            var setting = IfoodIntegrationSetting.Create(1).Value;
            setting.SaveCredentials("client-123", "original-secret", true, "customer-456");

            // Act
            var result = setting.SaveCredentials("client-999", blankSecret, false, "customer-999");

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.ClientId.Should().Be("client-999");
            setting.ClientSecretEncrypted.Should().Be("original-secret"); // kept, not overwritten
            setting.Enabled.Should().BeFalse();
            setting.IfoodCustomerId.Should().Be("customer-999");
        }

        [Fact]
        public void RegisterConnectionTest_WithSuccess_ShouldSetTimestampAndResult()
        {
            // Arrange
            var setting = IfoodIntegrationSetting.Create(1).Value;

            // Act
            setting.RegisterConnectionTest(true);

            // Assert
            setting.LastConnectionTestSucceeded.Should().BeTrue();
            setting.LastConnectionTestAt.Should().NotBeNull();
            setting.LastConnectionTestAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            setting.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void RegisterConnectionTest_WithFailure_ShouldSetTimestampAndResult()
        {
            // Arrange
            var setting = IfoodIntegrationSetting.Create(1).Value;

            // Act
            setting.RegisterConnectionTest(false);

            // Assert
            setting.LastConnectionTestSucceeded.Should().BeFalse();
            setting.LastConnectionTestAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodIntegrationSetting), true) as IfoodIntegrationSetting;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
