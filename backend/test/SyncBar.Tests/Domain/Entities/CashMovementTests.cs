using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CashMovementTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long cashSessionId = 1;
            long cashMovementTypeId = 3;
            long? saleId = 42;
            long employeeId = 7;
            decimal amount = 150.50m;
            string description = "Recebimento de venda";

            // Act
            var result = CashMovement.Create(cashSessionId, cashMovementTypeId, saleId, employeeId, amount, description);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var movement = result.Value;
            movement.Should().NotBeNull();
            movement.CashSessionId.Should().Be(cashSessionId);
            movement.CashMovementTypeId.Should().Be(cashMovementTypeId);
            movement.SaleId.Should().Be(saleId);
            movement.EmployeeId.Should().Be(employeeId);
            movement.Amount.Should().Be(amount);
            movement.Description.Should().Be(description);
            movement.IsActive.Should().BeTrue();
            movement.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            movement.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNullSaleIdAndDescription_ShouldReturnSuccessResult()
        {
            // Arrange & Act
            // No validation exists on CashMovement.Create, so a movement without a linked
            // sale (e.g. a manual "sangria"/"suprimento") and without a description is valid.
            var result = CashMovement.Create(1, 2, null, 7, 100m, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.SaleId.Should().BeNull();
            result.Value.Description.Should().BeNull();
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var movement = CashMovement.Create(1, 2, null, 7, 100m, null).Value;

            // Act
            movement.Touch();

            // Assert
            movement.UpdatedAt.Should().NotBeNull();
            movement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var movement = CashMovement.Create(1, 2, null, 7, 100m, null).Value;

            // Act
            movement.Deactivate();

            // Assert
            movement.IsActive.Should().BeFalse();
            movement.UpdatedAt.Should().NotBeNull();
            movement.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CashMovement), true) as CashMovement;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
