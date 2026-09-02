using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ProductComplementGroupTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long productId = 1;
            long complementGroupId = 5;
            int displayOrder = 2;

            // Act
            var result = ProductComplementGroup.Create(productId, complementGroupId, displayOrder);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var group = result.Value;
            group.Should().NotBeNull();
            group.ProductId.Should().Be(productId);
            group.ComplementGroupId.Should().Be(complementGroupId);
            group.DisplayOrder.Should().Be(displayOrder);
            group.IsActive.Should().BeTrue();
            group.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            group.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNegativeDisplayOrder_ShouldReturnFailureResult()
        {
            // Act
            var result = ProductComplementGroup.Create(1, 5, -1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ProductComplementGroup.InvalidDisplayOrder");
            result.Error.Message.Should().Be("Display order cannot be negative.");
        }

        [Fact]
        public void UpdateDisplayOrder_ShouldUpdateValueAndSetUpdatedAt()
        {
            // Arrange
            var group = ProductComplementGroup.Create(1, 5, 0).Value;

            // Act
            group.UpdateDisplayOrder(3);

            // Assert
            group.DisplayOrder.Should().Be(3);
            group.UpdatedAt.Should().NotBeNull();
            group.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var group = ProductComplementGroup.Create(1, 5, 0).Value;

            // Act
            group.Touch();

            // Assert
            group.UpdatedAt.Should().NotBeNull();
            group.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var group = ProductComplementGroup.Create(1, 5, 0).Value;

            // Act
            group.Deactivate();

            // Assert
            group.IsActive.Should().BeFalse();
            group.UpdatedAt.Should().NotBeNull();
            group.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ProductComplementGroup), true) as ProductComplementGroup;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
