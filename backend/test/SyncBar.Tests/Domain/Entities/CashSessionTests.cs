using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CashSessionTests
    {
        [Fact]
        public void Open_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long cashRegisterId = 1;
            long openedByEmployeeId = 7;
            decimal openingAmount = 200m;

            // Act
            var result = CashSession.Open(cashRegisterId, openedByEmployeeId, openingAmount);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var session = result.Value;
            session.Should().NotBeNull();
            session.CashRegisterId.Should().Be(cashRegisterId);
            session.OpenedByEmployeeId.Should().Be(openedByEmployeeId);
            session.OpeningAmount.Should().Be(openingAmount);
            session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Aberto);
            session.ClosedByEmployeeId.Should().BeNull();
            session.ClosingAmount.Should().BeNull();
            session.ExpectedAmount.Should().BeNull();
            session.DifferenceAmount.Should().BeNull();
            session.ClosedAt.Should().BeNull();
            session.OpenedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            session.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            session.UpdatedAt.Should().BeNull();
            session.IsActive.Should().BeTrue();
            session.IsOpen().Should().BeTrue();
        }

        [Fact]
        public void Open_WithNegativeOpeningAmount_ShouldReturnFailureResult()
        {
            // Act
            var result = CashSession.Open(1, 7, -1m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSession.InvalidOpeningAmount");
            result.Error.Message.Should().Be("Opening amount cannot be negative.");
        }

        [Fact]
        public void Close_WhenSessionIsOpen_ShouldReturnSuccessAndComputeDifferenceAmount()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;

            // Act
            // Closing = 520, expected = 500 => difference = +20 (surplus)
            var result = session.Close(closedByEmployeeId: 9, closingAmount: 520m, expectedAmount: 500m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            session.ClosedByEmployeeId.Should().Be(9);
            session.ClosingAmount.Should().Be(520m);
            session.ExpectedAmount.Should().Be(500m);
            session.DifferenceAmount.Should().Be(20m);
            session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Fechado);
            session.ClosedAt.Should().NotBeNull();
            session.ClosedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            session.UpdatedAt.Should().NotBeNull();
            session.IsOpen().Should().BeFalse();
        }

        [Fact]
        public void Close_WhenClosingAmountIsLessThanExpected_ShouldComputeNegativeDifferenceAmount()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;

            // Act
            // Closing = 480, expected = 500 => difference = -20 (shortage)
            var result = session.Close(closedByEmployeeId: 9, closingAmount: 480m, expectedAmount: 500m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            session.DifferenceAmount.Should().Be(-20m);
        }

        [Fact]
        public void Close_WithNegativeClosingAmount_ShouldReturnFailureResult()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;

            // Act
            var result = session.Close(closedByEmployeeId: 9, closingAmount: -1m, expectedAmount: 500m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSession.InvalidClosingAmount");
            result.Error.Message.Should().Be("Closing amount cannot be negative.");
            session.IsOpen().Should().BeTrue();
        }

        [Fact]
        public void Close_WhenAlreadyClosed_ShouldReturnFailureResult()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;
            session.Close(9, 520m, 500m);

            // Act
            var result = session.Close(9, 520m, 500m);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSession.NotOpen");
            result.Error.Message.Should().Be("Only an open session can be closed.");
        }

        [Fact]
        public void MarkAsReviewed_WhenSessionIsClosed_ShouldReturnSuccessAndSetStatusToConferido()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;
            session.Close(9, 520m, 500m);

            // Act
            var result = session.MarkAsReviewed();

            // Assert
            result.IsSuccess.Should().BeTrue();
            session.CashSessionStatusId.Should().Be(CashSessionStatusIds.Conferido);
            session.UpdatedAt.Should().NotBeNull();
        }

        [Fact]
        public void MarkAsReviewed_WhenSessionIsNotClosed_ShouldReturnFailureResult()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value; // still open, never closed

            // Act
            var result = session.MarkAsReviewed();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CashSession.NotClosed");
            result.Error.Message.Should().Be("Only a closed session can be reviewed.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var session = CashSession.Open(1, 7, 200m).Value;

            // Act
            session.Deactivate();

            // Assert
            session.IsActive.Should().BeFalse();
            session.UpdatedAt.Should().NotBeNull();
            session.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CashSession), true) as CashSession;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
