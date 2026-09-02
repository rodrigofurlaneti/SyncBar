using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // PizzaCrust.Create/UpdateDetails/Deactivate are declared `internal` in the Domain assembly —
    // they are meant to be invoked only from within the aggregate that owns pizza configuration
    // (same assembly), so this test relies on the Domain assembly granting the test assembly
    // internals access (established convention for this project's internal-only entities).
    public class PizzaCrustTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long pizzaConfigurationId = 1;
            string name = "Borda Recheada";
            decimal extraPrice = 8.00m;
            int displayOrder = 2;

            // Act
            var result = PizzaCrust.Create(pizzaConfigurationId, name, extraPrice, displayOrder);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var crust = result.Value;
            crust.Should().NotBeNull();
            crust.PizzaConfigurationId.Should().Be(pizzaConfigurationId);
            crust.Name.Should().Be(name);
            crust.ExtraPrice.Should().Be(extraPrice);
            crust.DisplayOrder.Should().Be(displayOrder);
            crust.IsActive.Should().BeTrue();
            crust.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            crust.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = PizzaCrust.Create(1, invalidName!, 5.00m, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaCrust.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Create_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Act
            var result = PizzaCrust.Create(1, "Borda Fina", -1.00m, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaCrust.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var crust = PizzaCrust.Create(1, "Borda Fina", 5.00m, 0).Value;

            // Act
            var result = crust.UpdateDetails("Borda Grossa", 7.50m, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            crust.Name.Should().Be("Borda Grossa");
            crust.ExtraPrice.Should().Be(7.50m);
            crust.DisplayOrder.Should().Be(1);
            crust.UpdatedAt.Should().NotBeNull();
            crust.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var crust = PizzaCrust.Create(1, "Borda Fina", 5.00m, 0).Value;

            // Act
            var result = crust.UpdateDetails(invalidName!, 5.00m, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaCrust.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
            crust.Name.Should().Be("Borda Fina");
        }

        [Fact]
        public void UpdateDetails_WithNegativeExtraPrice_ShouldReturnFailureResult()
        {
            // Arrange
            var crust = PizzaCrust.Create(1, "Borda Fina", 5.00m, 0).Value;

            // Act
            var result = crust.UpdateDetails("Borda Fina", -2.00m, 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaCrust.InvalidExtraPrice");
            result.Error.Message.Should().Be("Extra price cannot be negative.");
            crust.ExtraPrice.Should().Be(5.00m);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var crust = PizzaCrust.Create(1, "Borda Fina", 5.00m, 0).Value;

            // Act
            crust.Deactivate();

            // Assert
            crust.IsActive.Should().BeFalse();
            crust.UpdatedAt.Should().NotBeNull();
            crust.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaCrust), true) as PizzaCrust;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
