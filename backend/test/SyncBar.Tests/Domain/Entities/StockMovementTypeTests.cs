using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class StockMovementTypeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties(bool isInflow)
        {
            // Arrange
            string name = isInflow ? "Purchase Inflow" : "Sales Outflow";

            // Act
            var result = StockMovementType.Create(name, isInflow);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.IsInflow.Should().Be(isInflow);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = StockMovementType.Create(invalidName, true);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockMovementType.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var stockMovementType = StockMovementType.Create("Adjustment", true).Value;

            // Act
            stockMovementType.Touch();

            // Assert
            stockMovementType.UpdatedAt.Should().NotBeNull();
            stockMovementType.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var stockMovementType = StockMovementType.Create("Adjustment", false).Value;

            // Act
            stockMovementType.Deactivate();

            // Assert
            stockMovementType.IsActive.Should().BeFalse();
            stockMovementType.UpdatedAt.Should().NotBeNull();
            stockMovementType.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(StockMovementType), true) as StockMovementType;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
