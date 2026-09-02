using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // ProductStock is a plain entity (not Entity/AggregateRoot) whose public constructor is the
    // factory itself (no static Create/Result<ProductStock> wrapper exists in the source), so the
    // "Create" happy-path tests below exercise the constructor directly instead.
    public class ProductStockTests
    {
        [Fact]
        public void Constructor_WithValidArguments_ShouldSetPropertiesCorrectly()
        {
            // Arrange
            long productId = 42;
            decimal initialBalance = 100m;
            decimal minimumQuantity = 10m;

            // Act
            var stock = new ProductStock(productId, initialBalance, minimumQuantity);

            // Assert
            stock.ProductId.Should().Be(productId);
            stock.CurrentBalance.Should().Be(initialBalance);
            stock.MinimumQuantity.Should().Be(minimumQuantity);
            stock.IsActive.Should().BeTrue();
            stock.RowVersion.Should().BeEmpty();
            stock.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            stock.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Deduct_WithValidQuantity_ShouldReduceBalanceAndSetUpdatedAt()
        {
            // Arrange
            var stock = new ProductStock(1, 100m, 10m);

            // Act
            var result = stock.Deduct(30m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            stock.CurrentBalance.Should().Be(70m);
            stock.UpdatedAt.Should().NotBeNull();
            stock.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deduct_WithQuantityEqualToBalance_ShouldZeroOutBalance()
        {
            // Arrange
            var stock = new ProductStock(1, 50m, 0m);

            // Act
            var result = stock.Deduct(50m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            stock.CurrentBalance.Should().Be(0m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Deduct_WithZeroOrNegativeQuantity_ShouldReturnFailureResult(decimal invalidQuantity)
        {
            // Arrange
            var stock = new ProductStock(1, 100m, 10m);

            // Act
            var result = stock.Deduct(invalidQuantity);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Stock.InvalidQuantity");
            result.Error.Message.Should().Be("A quantidade a deduzir deve ser maior que zero.");
            stock.CurrentBalance.Should().Be(100m);
        }

        [Fact]
        public void Deduct_WithQuantityGreaterThanBalance_ShouldReturnFailureResult()
        {
            // Arrange
            var stock = new ProductStock(1, 20m, 5m);

            // Act
            var result = stock.Deduct(25m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Stock.Insufficient");
            result.Error.Message.Should().Contain("Estoque insuficiente");
            stock.CurrentBalance.Should().Be(20m);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ProductStock), true) as ProductStock;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeTrue();
        }
    }
}
