using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PurchaseItemTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long purchaseId = 100;
            long productId = 50;
            decimal quantity = 10.5m;
            decimal unitCost = 25.00m;
            decimal totalCost = 262.50m;

            // Act
            var result = PurchaseItem.Create(purchaseId, productId, quantity, unitCost, totalCost);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.PurchaseId.Should().Be(purchaseId);
            result.Value.ProductId.Should().Be(productId);
            result.Value.Quantity.Should().Be(quantity);
            result.Value.UnitCost.Should().Be(unitCost);
            result.Value.TotalCost.Should().Be(totalCost);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var purchaseItem = PurchaseItem.Create(100, 50, 2m, 10m, 20m).Value;

            // Act
            purchaseItem.Touch();

            // Assert
            purchaseItem.UpdatedAt.Should().NotBeNull();
            purchaseItem.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var purchaseItem = PurchaseItem.Create(100, 50, 2m, 10m, 20m).Value;

            // Act
            purchaseItem.Deactivate();

            // Assert
            purchaseItem.IsActive.Should().BeFalse();
            purchaseItem.UpdatedAt.Should().NotBeNull();
            purchaseItem.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PurchaseItem), true) as PurchaseItem;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}