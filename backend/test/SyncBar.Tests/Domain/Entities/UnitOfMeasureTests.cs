using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class UnitOfMeasureTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            string name = "Kilogram";
            string abbreviation = "kg";

            // Act
            var result = UnitOfMeasure.Create(name, abbreviation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.Abbreviation.Should().Be(abbreviation);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string invalidName)
        {
            // Act
            var result = UnitOfMeasure.Create(invalidName, "kg");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("UnitOfMeasure.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceAbbreviation_ShouldReturnFailureResult(string invalidAbbreviation)
        {
            // Act
            var result = UnitOfMeasure.Create("Kilogram", invalidAbbreviation);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("UnitOfMeasure.EmptyAbbreviation");
            result.Error.Message.Should().Be("Abbreviation is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var unit = UnitOfMeasure.Create("Liter", "L").Value;

            // Act
            unit.Touch();

            // Assert
            unit.UpdatedAt.Should().NotBeNull();
            unit.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var unit = UnitOfMeasure.Create("Liter", "L").Value;

            // Act
            unit.Deactivate();

            // Assert
            unit.IsActive.Should().BeFalse();
            unit.UpdatedAt.Should().NotBeNull();
            unit.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(UnitOfMeasure), true) as UnitOfMeasure;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
