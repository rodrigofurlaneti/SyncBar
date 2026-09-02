using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodProductMappingTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long productId = 100;
            long branchId = 1;

            // Act
            // No validation exists on IfoodProductMapping.Create — it always succeeds, generating
            // two fresh UUID v4 values (required by the Ifood Catalog API) for the item/product ids.
            var result = IfoodProductMapping.Create(productId, branchId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var mapping = result.Value;
            mapping.Should().NotBeNull();
            mapping.ProductId.Should().Be(productId);
            mapping.BranchId.Should().Be(branchId);
            mapping.IfoodItemId.Should().NotBe(Guid.Empty);
            mapping.IfoodProductId.Should().NotBe(Guid.Empty);
            mapping.IfoodItemId.Should().NotBe(mapping.IfoodProductId);
            mapping.IsActive.Should().BeTrue();
            mapping.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            mapping.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodProductMapping), true) as IfoodProductMapping;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
