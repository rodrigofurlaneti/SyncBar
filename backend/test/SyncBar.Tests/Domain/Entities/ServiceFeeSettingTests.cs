using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ServiceFeeSettingTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties(bool initialEnabledState)
        {
            // Arrange
            long branchId = 1;

            // Act
            var result = ServiceFeeSetting.Create(branchId, initialEnabledState);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.Enabled.Should().Be(initialEnabledState);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void SetEnabled_ShouldUpdateEnabledStateAndSetUpdatedAt(bool newEnabledState)
        {
            // Arrange
            var setting = ServiceFeeSetting.Create(1, !newEnabledState).Value;

            // Act
            var result = setting.SetEnabled(newEnabledState);

            // Assert
            result.IsSuccess.Should().BeTrue();
            setting.Enabled.Should().Be(newEnabledState);
            setting.UpdatedAt.Should().NotBeNull();
            setting.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ServiceFeeSetting), true) as ServiceFeeSetting;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
