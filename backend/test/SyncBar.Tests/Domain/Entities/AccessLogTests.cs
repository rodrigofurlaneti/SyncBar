using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class AccessLogTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long? appUserId = 10;
            string userName = "john.doe";
            string eventType = "LOGIN";
            string ipAddress = "127.0.0.1";
            string userAgent = "Mozilla/5.0";

            // Act
            var result = AccessLog.Create(appUserId, userName, eventType, ipAddress, userAgent);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var accessLog = result.Value;
            accessLog.Should().NotBeNull();
            accessLog.AppUserId.Should().Be(appUserId);
            accessLog.UserName.Should().Be(userName);
            accessLog.EventType.Should().Be(eventType);
            accessLog.IpAddress.Should().Be(ipAddress);
            accessLog.UserAgent.Should().Be(userAgent);
            accessLog.IsActive.Should().BeTrue();
            accessLog.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            accessLog.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithNullAppUserId_ShouldReturnSuccessResult()
        {
            // Act
            var result = AccessLog.Create(null, "anonymous", "LOGIN_FAILED", null, null);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.AppUserId.Should().BeNull();
            result.Value.IpAddress.Should().BeNull();
            result.Value.UserAgent.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceUserName_ShouldReturnFailureResult(string? invalidUserName)
        {
            // Act
            var result = AccessLog.Create(1, invalidUserName!, "LOGIN", null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AccessLog.EmptyUserName");
            result.Error.Message.Should().Be("UserName is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceEventType_ShouldReturnFailureResult(string? invalidEventType)
        {
            // Act
            var result = AccessLog.Create(1, "john.doe", invalidEventType!, null, null);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("AccessLog.EmptyEventType");
            result.Error.Message.Should().Be("EventType is required.");
        }

        [Fact]
        public void Touch_ShouldUpdateUpdatedAtTimestamp()
        {
            // Arrange
            var accessLog = AccessLog.Create(1, "john.doe", "LOGIN", null, null).Value;

            // Act
            accessLog.Touch();

            // Assert
            accessLog.UpdatedAt.Should().NotBeNull();
            accessLog.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var accessLog = AccessLog.Create(1, "john.doe", "LOGIN", null, null).Value;

            // Act
            accessLog.Deactivate();

            // Assert
            accessLog.IsActive.Should().BeFalse();
            accessLog.UpdatedAt.Should().NotBeNull();
            accessLog.UpdatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(AccessLog), true) as AccessLog;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
