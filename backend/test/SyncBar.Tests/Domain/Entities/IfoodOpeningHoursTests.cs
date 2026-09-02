using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodOpeningHoursTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            int dayOfWeek = 5;
            var start = TimeSpan.FromHours(18);
            int durationMinutes = 240;

            // Act
            var result = IfoodOpeningHours.Create(branchId, dayOfWeek, start, durationMinutes);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var openingHours = result.Value;
            openingHours.Should().NotBeNull();
            openingHours.BranchId.Should().Be(branchId);
            openingHours.DayOfWeek.Should().Be(dayOfWeek);
            openingHours.Start.Should().Be(start);
            openingHours.DurationMinutes.Should().Be(durationMinutes);
            openingHours.IsActive.Should().BeTrue();
            openingHours.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            openingHours.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(7)]
        public void Create_WithInvalidDayOfWeek_ShouldReturnFailureResult(int invalidDayOfWeek)
        {
            // Act
            var result = IfoodOpeningHours.Create(1, invalidDayOfWeek, TimeSpan.FromHours(10), 60);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOpeningHours.InvalidDayOfWeek");
            result.Error.Message.Should().Be("DayOfWeek must be between 0 (Sunday) and 6 (Saturday).");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Create_WithNonPositiveDuration_ShouldReturnFailureResult(int invalidDuration)
        {
            // Act
            var result = IfoodOpeningHours.Create(1, 3, TimeSpan.FromHours(10), invalidDuration);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOpeningHours.InvalidDuration");
            result.Error.Message.Should().Be("DurationMinutes must be greater than zero.");
        }

        [Fact]
        public void Create_WithNegativeStart_ShouldReturnFailureResult()
        {
            // Act
            var result = IfoodOpeningHours.Create(1, 3, TimeSpan.FromMinutes(-1), 60);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOpeningHours.InvalidStart");
            result.Error.Message.Should().Be("Start must be a valid time of day.");
        }

        [Fact]
        public void Create_WithStartAtOrBeyondOneDay_ShouldReturnFailureResult()
        {
            // Act
            var result = IfoodOpeningHours.Create(1, 3, TimeSpan.FromDays(1), 60);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOpeningHours.InvalidStart");
            result.Error.Message.Should().Be("Start must be a valid time of day.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var openingHours = IfoodOpeningHours.Create(1, 3, TimeSpan.FromHours(10), 60).Value;

            // Act
            openingHours.Deactivate();

            // Assert
            openingHours.IsActive.Should().BeFalse();
            openingHours.UpdatedAt.Should().NotBeNull();
            openingHours.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodOpeningHours), true) as IfoodOpeningHours;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
