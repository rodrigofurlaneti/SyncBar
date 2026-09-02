using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // PizzaEdge.Create/UpdateDetails/Deactivate são 'internal' (chamados de dentro do
    // aggregate PizzaConfiguration) — acessíveis aqui via InternalsVisibleTo de SyncBar.Domain
    // para SyncBar.Tests, mesmo padrão já usado para handlers 'internal sealed' da Application.
    public class PizzaEdgeTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long pizzaConfigurationId = 1;
            string name = "Catupiry";
            decimal extraPrice = 6.0m;
            int displayOrder = 1;

            // Act
            var result = PizzaEdge.Create(pizzaConfigurationId, name, extraPrice, displayOrder);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var pizzaEdge = result.Value;
            pizzaEdge.Should().NotBeNull();
            pizzaEdge.PizzaConfigurationId.Should().Be(pizzaConfigurationId);
            pizzaEdge.Name.Should().Be(name);
            pizzaEdge.ExtraPrice.Should().Be(extraPrice);
            pizzaEdge.DisplayOrder.Should().Be(displayOrder);
            pizzaEdge.IsActive.Should().BeTrue();
            pizzaEdge.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            pizzaEdge.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = PizzaEdge.Create(1, invalidName!, 6.0m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaEdge.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Create_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Act
            var result = PizzaEdge.Create(1, "Cheddar", -2.0m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaEdge.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var pizzaEdge = PizzaEdge.Create(1, "Catupiry", 6.0m, 1).Value;

            // Act
            var result = pizzaEdge.UpdateDetails("Cheddar", 7.5m, 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            pizzaEdge.Name.Should().Be("Cheddar");
            pizzaEdge.ExtraPrice.Should().Be(7.5m);
            pizzaEdge.DisplayOrder.Should().Be(2);
            pizzaEdge.UpdatedAt.Should().NotBeNull();
            pizzaEdge.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var pizzaEdge = PizzaEdge.Create(1, "Catupiry", 6.0m, 1).Value;

            // Act
            var result = pizzaEdge.UpdateDetails(invalidName!, 6.0m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaEdge.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Arrange
            var pizzaEdge = PizzaEdge.Create(1, "Catupiry", 6.0m, 1).Value;

            // Act
            var result = pizzaEdge.UpdateDetails("Catupiry", -1.0m, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaEdge.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var pizzaEdge = PizzaEdge.Create(1, "Catupiry", 6.0m, 1).Value;

            // Act
            pizzaEdge.Deactivate();

            // Assert
            pizzaEdge.IsActive.Should().BeFalse();
            pizzaEdge.UpdatedAt.Should().NotBeNull();
            pizzaEdge.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaEdge), true) as PizzaEdge;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
