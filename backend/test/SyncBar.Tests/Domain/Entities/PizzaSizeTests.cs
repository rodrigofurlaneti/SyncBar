using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // PizzaSize.Create, UpdateDetails and Deactivate are declared `internal` in the source, and no
    // InternalsVisibleTo attribute exposing them to this test assembly was found in the codebase.
    // PizzaSize is a child entity of the PizzaConfiguration aggregate root (same pattern as
    // Complement under ComplementGroup), so its behavior is exercised here through the aggregate's
    // public API (AddSize/UpdateSize/RemoveSize) — the only accessible entry point — while
    // asserting on PizzaSize's own public getters.
    public class PizzaSizeTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldProduceSizeWithCorrectProperties()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("Grande", 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var size = result.Value;
            size.PizzaConfigurationId.Should().Be(config.Id);
            size.Name.Should().Be("Grande");
            size.Slices.Should().Be(8);
            size.AcceptedFractions.Should().Be(2);
            size.DisplayOrder.Should().Be(1);
            size.IsActive.Should().BeTrue();
            size.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            size.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNullSlices_ShouldSucceed()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("Individual", null, 1, 1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Slices.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize(invalidName!, 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Create_WithAcceptedFractionsOutOfRange_ShouldReturnFailureResult(int invalidFractions)
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;

            // Act
            var result = config.AddSize("Grande", 8, invalidFractions, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.InvalidAcceptedFractions");
            result.Error.Message.Should().Be("Accepted fractions must be between 1 and 4.");
        }

        [Fact]
        public void UpdateDetails_WithValidArguments_ShouldUpdatePropertiesAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.UpdateSize(size.Id, "Familia", 12, 3, 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            size.Name.Should().Be("Familia");
            size.Slices.Should().Be(12);
            size.AcceptedFractions.Should().Be(3);
            size.DisplayOrder.Should().Be(2);
            size.UpdatedAt.Should().NotBeNull();
            size.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void UpdateDetails_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.UpdateSize(size.Id, invalidName!, 8, 2, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.EmptyName");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void UpdateDetails_WithAcceptedFractionsOutOfRange_ShouldReturnFailureResult(int invalidFractions)
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.UpdateSize(size.Id, "Grande", 8, invalidFractions, 1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("PizzaSize.InvalidAcceptedFractions");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var config = PizzaConfiguration.Create(1).Value;
            var size = config.AddSize("Grande", 8, 2, 1).Value;

            // Act
            var result = config.RemoveSize(size.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            size.IsActive.Should().BeFalse();
            size.UpdatedAt.Should().NotBeNull();
            size.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(PizzaSize), true) as PizzaSize;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
