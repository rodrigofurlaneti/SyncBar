using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class StockMovementTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long stockItemId = 1;
            long stockMovementTypeId = 2;
            long? purchaseItemId = 10;
            long? orderItemId = null;
            long? employeeId = 5;
            decimal quantity = 15.5m;
            decimal? unitCost = 10.00m;
            decimal? totalCost = 155.00m;
            string? documentNumber = "DOC-001";
            DateTime movedAt = DateTime.Now.AddHours(-1);
            string? notes = "Initial stock movement test";

            // Act
            var result = StockMovement.Create(
                stockItemId,
                stockMovementTypeId,
                purchaseItemId,
                orderItemId,
                employeeId,
                quantity,
                unitCost,
                totalCost,
                documentNumber,
                movedAt,
                notes);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.StockItemId.Should().Be(stockItemId);
            result.Value.StockMovementTypeId.Should().Be(stockMovementTypeId);
            result.Value.PurchaseItemId.Should().Be(purchaseItemId);
            result.Value.OrderItemId.Should().Be(orderItemId);
            result.Value.EmployeeId.Should().Be(employeeId);
            result.Value.Quantity.Should().Be(quantity);
            result.Value.UnitCost.Should().Be(unitCost);
            result.Value.TotalCost.Should().Be(totalCost);
            result.Value.DocumentNumber.Should().Be(documentNumber);
            result.Value.MovedAt.Should().Be(movedAt);
            result.Value.Notes.Should().Be(notes);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var stockMovement = StockMovement.Create(1, 1, null, null, null, 10, null, null, null, DateTime.Now, null).Value;

            // Act
            stockMovement.Touch();

            // Assert
            stockMovement.UpdatedAt.Should().NotBeNull();
            stockMovement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var stockMovement = StockMovement.Create(1, 1, null, null, null, 10, null, null, null, DateTime.Now, null).Value;

            // Act
            stockMovement.Deactivate();

            // Assert
            stockMovement.IsActive.Should().BeFalse();
            stockMovement.UpdatedAt.Should().NotBeNull();
            stockMovement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(StockMovement), true) as StockMovement;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
