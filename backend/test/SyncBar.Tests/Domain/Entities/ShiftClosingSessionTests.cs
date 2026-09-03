using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ShiftClosingSessionTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = ShiftClosingSession.Create(shiftClosingId: 1, cashSessionId: 2);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.ShiftClosingId.Should().Be(1);
            result.Value.CashSessionId.Should().Be(2);
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Create_WithInvalidShiftClosingId_ShouldReturnFailureResult()
        {
            // Act
            var result = ShiftClosingSession.Create(shiftClosingId: 0, cashSessionId: 2);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ShiftClosingSession.InvalidShiftClosing");
        }

        [Fact]
        public void Create_WithInvalidCashSessionId_ShouldReturnFailureResult()
        {
            // Act
            var result = ShiftClosingSession.Create(shiftClosingId: 1, cashSessionId: 0);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ShiftClosingSession.InvalidCashSession");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var link = ShiftClosingSession.Create(1, 2).Value;

            // Act
            link.Deactivate();

            // Assert
            link.IsActive.Should().BeFalse();
            link.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ShiftClosingSession), true) as ShiftClosingSession;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
