using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class DiningAreaAssignmentTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long diningAreaId = 1;
            long employeeId = 2;
            DateTime startAt = new DateTime(2026, 9, 2, 8, 0, 0);

            // Act
            var result = DiningAreaAssignment.Create(diningAreaId, employeeId, startAt);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.DiningAreaId.Should().Be(diningAreaId);
            result.Value.EmployeeId.Should().Be(employeeId);
            result.Value.StartAt.Should().Be(startAt);
            result.Value.EndAt.Should().BeNull();
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidDiningAreaId_ShouldReturnFailureResult(long invalidDiningAreaId)
        {
            // Act
            var result = DiningAreaAssignment.Create(invalidDiningAreaId, 2, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningAreaAssignment.InvalidDiningAreaId");
            result.Error.Message.Should().Be("DiningAreaId must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidEmployeeId_ShouldReturnFailureResult(long invalidEmployeeId)
        {
            // Act
            var result = DiningAreaAssignment.Create(1, invalidEmployeeId, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningAreaAssignment.InvalidEmployeeId");
            result.Error.Message.Should().Be("EmployeeId must be greater than zero.");
        }

        [Fact]
        public void Create_WithDefaultStartAt_ShouldReturnFailureResult()
        {
            // Act
            var result = DiningAreaAssignment.Create(1, 2, default);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("DiningAreaAssignment.InvalidStartAt");
            result.Error.Message.Should().Be("StartAt must be a valid date and time.");
        }

        [Fact]
        public void EndAssignment_ShouldSetEndAtAndUpdatedAt()
        {
            // Arrange
            var assignment = DiningAreaAssignment.Create(1, 2, DateTime.Now).Value;
            var endAt = DateTime.Now.AddHours(4);

            // Act
            assignment.EndAssignment(endAt);

            // Assert
            assignment.EndAt.Should().Be(endAt);
            assignment.UpdatedAt.Should().NotBeNull();
            assignment.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var assignment = DiningAreaAssignment.Create(1, 2, DateTime.Now).Value;

            // Act
            assignment.Touch();

            // Assert
            assignment.UpdatedAt.Should().NotBeNull();
            assignment.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var assignment = DiningAreaAssignment.Create(1, 2, DateTime.Now).Value;

            // Act
            assignment.Deactivate();

            // Assert
            assignment.IsActive.Should().BeFalse();
            assignment.UpdatedAt.Should().NotBeNull();
            assignment.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(DiningAreaAssignment), true) as DiningAreaAssignment;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
