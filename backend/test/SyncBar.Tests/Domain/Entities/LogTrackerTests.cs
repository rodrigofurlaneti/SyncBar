using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    // LogTracker has no static Create factory (used internally by BaseCommandHandler /
    // BaseQueryHandler / ApiController on every request) — it exposes a public constructor that
    // takes an id plus a set of public get/set properties. Tests below exercise that public
    // constructor and the property round-trips, which are its only testable public behavior.
    public class LogTrackerTests
    {
        [Fact]
        public void Constructor_WithId_ShouldSetIdAndInitializeDefaults()
        {
            // Arrange & Act
            var logTracker = new LogTracker(42);

            // Assert
            logTracker.Id.Should().Be(42);
            logTracker.ClassName.Should().Be(string.Empty);
            logTracker.MethodName.Should().Be(string.Empty);
            logTracker.IsActive.Should().BeTrue();
            logTracker.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
            logTracker.UpdatedAt.Should().BeNull();
            logTracker.AppUserId.Should().BeNull();
            logTracker.DirectoryName.Should().BeNull();
            logTracker.Message.Should().BeNull();
            logTracker.ErrorMessage.Should().BeNull();
            logTracker.StackTrace.Should().BeNull();
            logTracker.IpAddress.Should().BeNull();
            logTracker.ExecutionTimeMs.Should().BeNull();
        }

        [Fact]
        public void Properties_ShouldBeSettableAndReturnAssignedValues()
        {
            // Arrange
            var logTracker = new LogTracker(1);
            var createdAt = DateTime.Now.AddMinutes(-10);
            var updatedAt = DateTime.Now;

            // Act
            logTracker.AppUserId = 7;
            logTracker.DirectoryName = "Application.Sales";
            logTracker.ClassName = "RegisterSaleCommandHandler";
            logTracker.MethodName = "Handle";
            logTracker.IsSuccess = true;
            logTracker.ExecutionTimeMs = 123;
            logTracker.Message = "Sale registered.";
            logTracker.ErrorMessage = "boom";
            logTracker.StackTrace = "at Foo.Bar()";
            logTracker.IpAddress = "10.0.0.1";
            logTracker.CreatedAt = createdAt;
            logTracker.UpdatedAt = updatedAt;
            logTracker.IsActive = false;

            // Assert
            logTracker.AppUserId.Should().Be(7);
            logTracker.DirectoryName.Should().Be("Application.Sales");
            logTracker.ClassName.Should().Be("RegisterSaleCommandHandler");
            logTracker.MethodName.Should().Be("Handle");
            logTracker.IsSuccess.Should().BeTrue();
            logTracker.ExecutionTimeMs.Should().Be(123);
            logTracker.Message.Should().Be("Sale registered.");
            logTracker.ErrorMessage.Should().Be("boom");
            logTracker.StackTrace.Should().Be("at Foo.Bar()");
            logTracker.IpAddress.Should().Be("10.0.0.1");
            logTracker.CreatedAt.Should().Be(createdAt);
            logTracker.UpdatedAt.Should().Be(updatedAt);
            logTracker.IsActive.Should().BeFalse();
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(LogTracker), true) as LogTracker;

            // Assert
            instance.Should().NotBeNull();
            instance!.Id.Should().Be(0);
            instance.IsActive.Should().BeTrue(); // default field initializer, unlike other entities
        }
    }
}
