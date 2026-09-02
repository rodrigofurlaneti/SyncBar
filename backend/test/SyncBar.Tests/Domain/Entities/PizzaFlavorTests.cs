using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class PizzaFlavorTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long companyId = 1;
            string name = "Calabresa";
            string description = "Calabresa, cebola e azeitona";

            // Act
            var result = PizzaFlavor.Create(companyId, name, description);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var flavor = result.Value;
            flavor.Should().NotBeNull();
            flavor.CompanyId.Should().Be(companyId);
            flavor.Name.Should().Be(name);
            flavor.Description.Should().Be(description);
            flavor.ImageUrl.Should().BeNull();
            flavor.IsActive.Should().BeTrue();
            flavor.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            flavor.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = PizzaFlavor.Create(1, invalidName, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaFlavor.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdateNameAndDescription()
        {
            // Arrange
            var flavor = PizzaFlavor.Create(1, "Calabresa", "Descrição antiga").Value;

            // Act
            var result = flavor.UpdateDetails("Frango Catupiry", "Nova descrição");

            // Assert
            result.IsSuccess.Should().BeTrue();
            flavor.Name.Should().Be("Frango Catupiry");
            flavor.Description.Should().Be("Nova descrição");
            flavor.UpdatedAt.Should().NotBeNull();
            flavor.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var flavor = PizzaFlavor.Create(1, "Calabresa", "Descrição").Value;

            // Act
            var result = flavor.UpdateDetails(invalidName, "Nova descrição");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaFlavor.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
            flavor.Name.Should().Be("Calabresa");
        }

        [Fact]
        public void SetImage_ShouldUpdateImageUrlAndSetUpdatedAt()
        {
            // Arrange
            var flavor = PizzaFlavor.Create(1, "Calabresa", null).Value;

            // Act
            flavor.SetImage("https://example.com/calabresa.png");

            // Assert
            flavor.ImageUrl.Should().Be("https://example.com/calabresa.png");
            flavor.UpdatedAt.Should().NotBeNull();
            flavor.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var flavor = PizzaFlavor.Create(1, "Calabresa", null).Value;

            // Act
            flavor.Deactivate();

            // Assert
            flavor.IsActive.Should().BeFalse();
            flavor.UpdatedAt.Should().NotBeNull();
            flavor.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaFlavor), true) as PizzaFlavor;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
