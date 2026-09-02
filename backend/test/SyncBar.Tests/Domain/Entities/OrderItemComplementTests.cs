using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // Create and Deactivate are declared `internal` on OrderItemComplement (called only from
    // within the Domain assembly, e.g. OrderItem) — assumed reachable here via
    // InternalsVisibleTo(SyncBar.Tests) configured on the Domain project.
    public class OrderItemComplementTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long orderItemId = 1;
            long complementId = 2;
            decimal unitPriceCharged = 5.50m;
            var now = DateTime.Now;

            // Act
            var result = OrderItemComplement.Create(orderItemId, complementId, unitPriceCharged, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var complement = result.Value;
            complement.Should().NotBeNull();
            complement.OrderItemId.Should().Be(orderItemId);
            complement.ComplementId.Should().Be(complementId);
            complement.UnitPriceCharged.Should().Be(unitPriceCharged);
            complement.IsActive.Should().BeTrue();
            complement.CreatedAt.Should().Be(now);
            complement.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithZeroPrice_ShouldReturnSuccessResult()
        {
            // Act
            var result = OrderItemComplement.Create(1, 2, 0m, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.UnitPriceCharged.Should().Be(0m);
        }

        [Fact]
        public void Create_WithNegativePrice_ShouldReturnFailureResult()
        {
            // Act
            var result = OrderItemComplement.Create(1, 2, -0.01m, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItemComplement.InvalidPrice");
            result.Error.Message.Should().Be("Price charged cannot be negative.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var complement = OrderItemComplement.Create(1, 2, 5.50m, DateTime.Now).Value;
            var deactivatedAt = DateTime.Now.AddMinutes(1);

            // Act
            complement.Deactivate(deactivatedAt);

            // Assert
            complement.IsActive.Should().BeFalse();
            complement.UpdatedAt.Should().Be(deactivatedAt);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OrderItemComplement), true) as OrderItemComplement;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
