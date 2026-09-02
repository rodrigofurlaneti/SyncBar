using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodMerchantMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;

            // Act
            // No validation exists on IfoodMerchantMapping.Create — branchId is unconstrained here.
            var result = IfoodMerchantMapping.Create(branchId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.BranchId.Should().Be(branchId);
            mapping.MerchantId.Should().BeNull();
            mapping.MerchantUuid.Should().BeNull();
            mapping.PreparationTimeMinutes.Should().BeNull();
            mapping.IsActive.Should().BeTrue();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void SetMerchant_WithValidArguments_ShouldUpdateMerchantFieldsAndSetUpdatedAt()
        {
            // Arrange
            var mapping = IfoodMerchantMapping.Create(1).Value;
            string merchantId = "merchant-abc";
            string merchantUuid = "11111111-1111-1111-1111-111111111111";

            // Act
            var result = mapping.SetMerchant(merchantId, merchantUuid);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mapping.MerchantId.Should().Be(merchantId);
            mapping.MerchantUuid.Should().Be(merchantUuid);
            mapping.UpdatedAt.Should().NotBeNull();
            mapping.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void SetPreparationTime_WithValidMinutes_ShouldReturnSuccessAndUpdateValue()
        {
            // Arrange
            var mapping = IfoodMerchantMapping.Create(1).Value;

            // Act
            var result = mapping.SetPreparationTime(30);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mapping.PreparationTimeMinutes.Should().Be(30);
            mapping.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetPreparationTime_WithNull_ShouldReturnSuccessAndClearCustomization()
        {
            // Arrange
            var mapping = IfoodMerchantMapping.Create(1).Value;
            mapping.SetPreparationTime(30);

            // Act
            // Null represents removing the customization (falls back to Ifood's automatic estimate).
            var result = mapping.SetPreparationTime(null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            mapping.PreparationTimeMinutes.Should().BeNull();
        }

        [Fact]
        public void SetPreparationTime_WithNegativeMinutes_ShouldReturnFailureResult()
        {
            // Arrange
            var mapping = IfoodMerchantMapping.Create(1).Value;

            // Act
            var result = mapping.SetPreparationTime(-5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodMerchantMapping.InvalidPreparationTime");
            result.Error.Message.Should().Be("Preparation time cannot be negative.");
            mapping.PreparationTimeMinutes.Should().BeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodMerchantMapping), true) as IfoodMerchantMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
