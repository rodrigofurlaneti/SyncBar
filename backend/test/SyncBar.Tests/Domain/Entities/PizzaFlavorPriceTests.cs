using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // PizzaFlavorPrice.Create/UpdatePrice/Deactivate are declared `internal` — accessible here via
    // InternalsVisibleTo(SyncBar.Tests) configured on the Domain project.
    public class PizzaFlavorPriceTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long pizzaConfigurationId = 1;
            long pizzaFlavorId = 2;
            long pizzaSizeId = 3;
            decimal price = 45.90m;

            // Act
            var result = PizzaFlavorPrice.Create(pizzaConfigurationId, pizzaFlavorId, pizzaSizeId, price);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.PizzaConfigurationId.Should().Be(pizzaConfigurationId);
            result.Value.PizzaFlavorId.Should().Be(pizzaFlavorId);
            result.Value.PizzaSizeId.Should().Be(pizzaSizeId);
            result.Value.Price.Should().Be(price);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNegativePrice_ShouldReturnFailureResult()
        {
            // Act
            var result = PizzaFlavorPrice.Create(1, 2, 3, -0.01m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaFlavorPrice.InvalidPrice");
            result.Error.Message.Should().Be("Price cannot be negative.");
        }

        [Fact]
        public void UpdatePrice_WithValidPrice_ShouldUpdatePriceAndSetUpdatedAt()
        {
            // Arrange
            var flavorPrice = PizzaFlavorPrice.Create(1, 2, 3, 40.00m).Value;

            // Act
            var result = flavorPrice.UpdatePrice(49.90m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            flavorPrice.Price.Should().Be(49.90m);
            flavorPrice.UpdatedAt.Should().NotBeNull();
            flavorPrice.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void UpdatePrice_WithNegativePrice_ShouldReturnFailureResult()
        {
            // Arrange
            var flavorPrice = PizzaFlavorPrice.Create(1, 2, 3, 40.00m).Value;

            // Act
            var result = flavorPrice.UpdatePrice(-5m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaFlavorPrice.InvalidPrice");
            result.Error.Message.Should().Be("Price cannot be negative.");
            flavorPrice.Price.Should().Be(40.00m);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var flavorPrice = PizzaFlavorPrice.Create(1, 2, 3, 40.00m).Value;

            // Act
            flavorPrice.Deactivate();

            // Assert
            flavorPrice.IsActive.Should().BeFalse();
            flavorPrice.UpdatedAt.Should().NotBeNull();
            flavorPrice.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaFlavorPrice), true) as PizzaFlavorPrice;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
