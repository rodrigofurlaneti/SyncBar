using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class StockItemTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long productId = 10;
            decimal minimumQuantity = 5m;
            decimal? maximumQuantity = 100m;

            // Act
            var result = StockItem.Create(branchId, productId, minimumQuantity, maximumQuantity);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.ProductId.Should().Be(productId);
            result.Value.CurrentQuantity.Should().Be(0); // Inicia sempre com zero
            result.Value.MinimumQuantity.Should().Be(minimumQuantity);
            result.Value.MaximumQuantity.Should().Be(maximumQuantity);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNegativeMinimumQuantity_ShouldReturnFailureResult()
        {
            // Act
            var result = StockItem.Create(1, 10, -1m, 100m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InvalidMinimum");
            result.Error.Message.Should().Be("Minimum quantity cannot be negative.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Increase_WithInvalidQuantity_ShouldReturnFailureResult(decimal invalidQuantity)
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;

            // Act
            var result = stockItem.Increase(invalidQuantity);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InvalidQuantity");
            result.Error.Message.Should().Be("Quantity must be greater than zero.");
        }

        [Fact]
        public void Increase_WithValidQuantity_ShouldIncreaseCurrentQuantityAndSetUpdatedAt()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;
            decimal quantityToAdd = 20m;

            // Act
            var result = stockItem.Increase(quantityToAdd);

            // Assert
            result.IsSuccess.Should().BeTrue();
            stockItem.CurrentQuantity.Should().Be(20m);
            stockItem.UpdatedAt.Should().NotBeNull();
            stockItem.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Decrease_WithInvalidQuantity_ShouldReturnFailureResult(decimal invalidQuantity)
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;
            stockItem.Increase(50); // Current is 50

            // Act
            var result = stockItem.Decrease(invalidQuantity);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InvalidQuantity");
            result.Error.Message.Should().Be("Quantity must be greater than zero.");
        }

        [Fact]
        public void Decrease_WhenResultingStockWouldBeNegative_ShouldReturnFailureResult()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;
            stockItem.Increase(10); // Current is 10

            // Act (trying to decrease 15, leaving -5)
            var result = stockItem.Decrease(15m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InsufficientStock");
            result.Error.Message.Should().Be("Stock cannot become negative.");
        }

        [Fact]
        public void Decrease_WithValidQuantity_ShouldDecreaseCurrentQuantityAndSetUpdatedAt()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;
            stockItem.Increase(30); // Current is 30

            // Act
            var result = stockItem.Decrease(10m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            stockItem.CurrentQuantity.Should().Be(20m);
            stockItem.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void SetLimits_WithNegativeMinimum_ShouldReturnFailureResult()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;

            // Act
            var result = stockItem.SetLimits(-2m, 50m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InvalidMinimum");
        }

        [Fact]
        public void SetLimits_WithMaximumBelowMinimum_ShouldReturnFailureResult()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;

            // Act (minimum = 10, maximum = 5)
            var result = stockItem.SetLimits(10m, 5m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("StockItem.InvalidMaximum");
            result.Error.Message.Should().Be("Maximum cannot be below minimum.");
        }

        [Fact]
        public void SetLimits_WithValidArguments_ShouldUpdateLimitsAndSetUpdatedAt()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;

            // Act
            var result = stockItem.SetLimits(10m, 200m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            stockItem.MinimumQuantity.Should().Be(10m);
            stockItem.MaximumQuantity.Should().Be(200m);
            stockItem.UpdatedAt.Should().NotBeNull();
        }

        [Theory]
        [InlineData(2, 5, true)]   // Current (2) < Minimum (5) -> True
        [InlineData(5, 5, false)]  // Current (5) < Minimum (5) -> False
        [InlineData(10, 5, false)] // Current (10) < Minimum (5) -> False
        public void IsBelowMinimum_ShouldReturnExpectedResult(decimal currentQty, decimal minQty, bool expectedResult)
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, minQty, 100).Value;
            if (currentQty > 0)
            {
                stockItem.Increase(currentQty);
            }

            // Act
            var result = stockItem.IsBelowMinimum();

            // Assert
            result.Should().Be(expectedResult);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var stockItem = StockItem.Create(1, 10, 5, 100).Value;

            // Act
            stockItem.Deactivate();

            // Assert
            stockItem.IsActive.Should().BeFalse();
            stockItem.UpdatedAt.Should().NotBeNull();
            stockItem.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(StockItem), true) as StockItem;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
