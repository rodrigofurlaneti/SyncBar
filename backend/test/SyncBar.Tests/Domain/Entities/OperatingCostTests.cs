using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class OperatingCostTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long costTypeId = 2;
            string description = "Aluguel";
            decimal amount = 1500.50m;
            int year = 2026;
            int month = 9;

            // Act
            var result = OperatingCost.Create(branchId, costTypeId, description, amount, year, month);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var cost = result.Value;
            cost.Should().NotBeNull();
            cost.BranchId.Should().Be(branchId);
            cost.CostTypeId.Should().Be(costTypeId);
            cost.Description.Should().Be(description);
            cost.Amount.Should().Be(amount);
            cost.ReferenceYear.Should().Be(year);
            cost.ReferenceMonth.Should().Be(month);
            cost.IsActive.Should().BeTrue();
            cost.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            cost.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceDescription_ShouldReturnFailureResult(string? invalidDescription)
        {
            // Act
            var result = OperatingCost.Create(1, 2, invalidDescription!, 100m, 2026, 9);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OperatingCost.EmptyDescription");
            result.Error.Message.Should().Be("Description is required.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Create_WithInvalidAmount_ShouldReturnFailureResult(decimal invalidAmount)
        {
            // Act
            var result = OperatingCost.Create(1, 2, "Aluguel", invalidAmount, 2026, 9);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OperatingCost.InvalidAmount");
            result.Error.Message.Should().Be("Amount must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void Create_WithInvalidMonth_ShouldReturnFailureResult(int invalidMonth)
        {
            // Act
            var result = OperatingCost.Create(1, 2, "Aluguel", 100m, 2026, invalidMonth);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OperatingCost.InvalidMonth");
            result.Error.Message.Should().Be("Reference month must be between 1 and 12.");
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        public void Create_WithInvalidYear_ShouldReturnFailureResult(int invalidYear)
        {
            // Act
            var result = OperatingCost.Create(1, 2, "Aluguel", 100m, invalidYear, 9);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OperatingCost.InvalidYear");
            result.Error.Message.Should().Be("Reference year out of range.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var cost = OperatingCost.Create(1, 2, "Aluguel", 100m, 2026, 9).Value;

            // Act
            cost.Deactivate();

            // Assert
            cost.IsActive.Should().BeFalse();
            cost.UpdatedAt.Should().NotBeNull();
            cost.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OperatingCost), true) as OperatingCost;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
