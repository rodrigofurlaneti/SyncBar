using SyncBar.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class RevenueTargetTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            int referenceYear = 2026;
            int referenceMonth = 8;
            decimal targetAmount = 50000.00m;

            // Act
            var result = RevenueTarget.Create(branchId, referenceYear, referenceMonth, targetAmount);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.ReferenceYear.Should().Be(referenceYear);
            result.Value.ReferenceMonth.Should().Be(referenceMonth);
            result.Value.TargetAmount.Should().Be(targetAmount);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1500.00)]
        public void Create_WithInvalidTargetAmount_ShouldReturnFailureResult(decimal invalidAmount)
        {
            // Act
            var result = RevenueTarget.Create(1, 2026, 8, invalidAmount);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RevenueTarget.InvalidAmount");
            result.Error.Message.Should().Be("Target must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        [InlineData(-5)]
        public void Create_WithInvalidReferenceMonth_ShouldReturnFailureResult(int invalidMonth)
        {
            // Act
            var result = RevenueTarget.Create(1, 2026, invalidMonth, 50000.00m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RevenueTarget.InvalidMonth");
            result.Error.Message.Should().Be("Reference month must be between 1 and 12.");
        }

        [Theory]
        [InlineData(1999)]
        [InlineData(2101)]
        public void Create_WithInvalidReferenceYear_ShouldReturnFailureResult(int invalidYear)
        {
            // Act
            var result = RevenueTarget.Create(1, invalidYear, 8, 50000.00m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RevenueTarget.InvalidYear");
            result.Error.Message.Should().Be("Reference year out of range.");
        }

        [Fact]
        public void UpdateAmount_WithValidAmount_ShouldUpdateTargetAndSetUpdatedAt()
        {
            // Arrange
            var revenueTarget = RevenueTarget.Create(1, 2026, 8, 50000.00m).Value;
            decimal newTargetAmount = 75000.00m;

            // Act
            var result = revenueTarget.UpdateAmount(newTargetAmount);

            // Assert
            result.IsSuccess.Should().BeTrue();
            revenueTarget.TargetAmount.Should().Be(newTargetAmount);
            revenueTarget.UpdatedAt.Should().NotBeNull();
            revenueTarget.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100.00)]
        public void UpdateAmount_WithInvalidAmount_ShouldReturnFailureResult(decimal invalidAmount)
        {
            // Arrange
            var revenueTarget = RevenueTarget.Create(1, 2026, 8, 50000.00m).Value;

            // Act
            var result = revenueTarget.UpdateAmount(invalidAmount);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("RevenueTarget.InvalidAmount");
            result.Error.Message.Should().Be("Target must be greater than zero.");

            // Garante que o valor antigo não foi alterado
            revenueTarget.TargetAmount.Should().Be(50000.00m);
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var revenueTarget = RevenueTarget.Create(1, 2026, 8, 50000.00m).Value;

            // Act
            revenueTarget.Deactivate();

            // Assert
            revenueTarget.IsActive.Should().BeFalse();
            revenueTarget.UpdatedAt.Should().NotBeNull();
            revenueTarget.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(RevenueTarget), true) as RevenueTarget;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
