using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CashMovementTypeTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties(bool isInflow)
        {
            // Arrange
            string name = isInflow ? "Suprimento" : "Sangria";

            // Act
            var result = CashMovementType.Create(name, isInflow);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Name.Should().Be(name);
            result.Value.IsInflow.Should().Be(isInflow);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = CashMovementType.Create(invalidName!, true);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashMovementType.EmptyName");
            result.Error.Message.Should().Be("Name is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var cashMovementType = CashMovementType.Create("Despesa", false).Value;

            // Act
            cashMovementType.Touch();

            // Assert
            cashMovementType.UpdatedAt.Should().NotBeNull();
            cashMovementType.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var cashMovementType = CashMovementType.Create("Estorno de Venda", true).Value;

            // Act
            cashMovementType.Deactivate();

            // Assert
            cashMovementType.IsActive.Should().BeFalse();
            cashMovementType.UpdatedAt.Should().NotBeNull();
            cashMovementType.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CashMovementType), true) as CashMovementType;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
