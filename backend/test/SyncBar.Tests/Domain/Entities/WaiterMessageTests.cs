using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class WaiterMessageTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long branchId = 1;
            long senderId = 10;
            long? recipientId = 20;
            long diningAreaId = 5;
            string message = "  Hello, table 5 needs assistance.  ";

            // Act
            var result = WaiterMessage.Create(branchId, senderId, recipientId, diningAreaId, message);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.BranchId.Should().Be(branchId);
            result.Value.SenderEmployeeId.Should().Be(senderId);
            result.Value.RecipientEmployeeId.Should().Be(recipientId);
            result.Value.DiningAreaId.Should().Be(diningAreaId);
            result.Value.Message.Should().Be("Hello, table 5 needs assistance."); // Validates Trim()
            result.Value.IsRead.Should().BeFalse();
            result.Value.IsActive.Should().BeTrue();
            result.Value.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            result.Value.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidBranchId_ShouldReturnFailureResult(long invalidBranchId)
        {
            // Act
            var result = WaiterMessage.Create(invalidBranchId, 10, null, 5, "Valid message");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("WaiterMessage.InvalidBranchId");
            result.Error.Message.Should().Be("BranchId must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public void Create_WithInvalidSenderEmployeeId_ShouldReturnFailureResult(long invalidSenderId)
        {
            // Act
            var result = WaiterMessage.Create(1, invalidSenderId, null, 5, "Valid message");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("WaiterMessage.InvalidSenderId");
            result.Error.Message.Should().Be("SenderEmployeeId must be greater than zero.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-10)]
        public void Create_WithInvalidDiningAreaId_ShouldReturnFailureResult(long invalidDiningAreaId)
        {
            // Act
            var result = WaiterMessage.Create(1, 10, null, invalidDiningAreaId, "Valid message");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("WaiterMessage.InvalidDiningAreaId");
            result.Error.Message.Should().Be("DiningAreaId cannot be null or zero.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceMessage_ShouldReturnFailureResult(string? invalidMessage)
        {
            // Act
            var result = WaiterMessage.Create(1, 10, null, 5, invalidMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("WaiterMessage.EmptyMessage");
            result.Error.Message.Should().Be("Message content cannot be empty.");
        }

        [Fact]
        public void MarkAsRead_ShouldUpdateIsReadToTrueAndSetUpdatedAt()
        {
            // Arrange
            var messageResult = WaiterMessage.Create(1, 10, 20, 5, "Test message");
            var waiterMessage = messageResult.Value;

            // Act
            waiterMessage.MarkAsRead();

            // Assert
            waiterMessage.IsRead.Should().BeTrue();
            waiterMessage.UpdatedAt.Should().NotBeNull();
            waiterMessage.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var messageResult = WaiterMessage.Create(1, 10, null, 5, "Test message");
            var waiterMessage = messageResult.Value;

            // Act
            waiterMessage.Deactivate();

            // Assert
            waiterMessage.IsActive.Should().BeFalse();
            waiterMessage.UpdatedAt.Should().NotBeNull();
            waiterMessage.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(WaiterMessage), true) as WaiterMessage;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse(); // Default initialization or from base if applicable
        }
    }
}
