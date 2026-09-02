using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // Create and Deactivate are declared `internal` on OrderItemPizzaFlavor (called only from
    // within the Domain assembly, e.g. OrderItem.CreatePizza) — assumed reachable here via
    // InternalsVisibleTo(SyncBar.Tests) configured on the Domain project.
    public class OrderItemPizzaFlavorTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long orderItemId = 1;
            long pizzaFlavorId = 2;
            decimal fractionShare = 0.5m;
            var now = DateTime.Now;

            // Act
            var result = OrderItemPizzaFlavor.Create(orderItemId, pizzaFlavorId, fractionShare, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var flavor = result.Value;
            flavor.Should().NotBeNull();
            flavor.OrderItemId.Should().Be(orderItemId);
            flavor.PizzaFlavorId.Should().Be(pizzaFlavorId);
            flavor.FractionShare.Should().Be(fractionShare);
            flavor.IsActive.Should().BeTrue();
            flavor.CreatedAt.Should().Be(now);
        }

        [Fact]
        public void Create_WithFullFraction_ShouldReturnSuccessResult()
        {
            // Act
            var result = OrderItemPizzaFlavor.Create(1, 2, 1m, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.FractionShare.Should().Be(1m);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-0.1)]
        [InlineData(1.01)]
        public void Create_WithFractionShareOutOfRange_ShouldReturnFailureResult(double invalidFraction)
        {
            // Act
            var result = OrderItemPizzaFlavor.Create(1, 2, (decimal)invalidFraction, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItemPizzaFlavor.InvalidFraction");
            result.Error.Message.Should().Be("Fraction share must be between 0 (exclusive) and 1 (inclusive).");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalse()
        {
            // Arrange
            var flavor = OrderItemPizzaFlavor.Create(1, 2, 0.5m, DateTime.Now).Value;

            // Act
            flavor.Deactivate(DateTime.Now);

            // Assert
            flavor.IsActive.Should().BeFalse();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OrderItemPizzaFlavor), true) as OrderItemPizzaFlavor;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
