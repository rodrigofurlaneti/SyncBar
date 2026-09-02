using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodLogisticsDeliveryTests
    {
        private static readonly DateTime Now = new(2026, 9, 2, 12, 0, 0);

        private static IfoodLogisticsDelivery CreateValidDelivery()
            => IfoodLogisticsDelivery.Create(1, 1, "Joao Silva", "11999998888", "MOTORCYCLE", Now).Value;

        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange & Act
            // Trailing/leading whitespace validates the .Trim() applied to the driver fields.
            var result = IfoodLogisticsDelivery.Create(10, 1, "  Joao Silva  ", "  11999998888  ", "  MOTORCYCLE  ", Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var delivery = result.Value;
            delivery.Should().NotBeNull();
            delivery.IfoodOrderId.Should().Be(10);
            delivery.BranchId.Should().Be(1);
            delivery.DriverName.Should().Be("Joao Silva");
            delivery.DriverPhone.Should().Be("11999998888");
            delivery.DriverVehicleType.Should().Be("MOTORCYCLE");
            delivery.Status.Should().Be(IfoodLogisticsStatuses.DriverAssigned);
            delivery.AssignedAt.Should().Be(Now);
            delivery.CreatedAt.Should().Be(Now);
            delivery.GoingToOriginAt.Should().BeNull();
            delivery.ArrivedAtOriginAt.Should().BeNull();
            delivery.DispatchedAt.Should().BeNull();
            delivery.ArrivedAtDestinationAt.Should().BeNull();
            delivery.DeliveryCodeVerifiedAt.Should().BeNull();
            delivery.IsActive.Should().BeTrue();
            delivery.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceDriverName_ShouldReturnFailureResult(string? invalidName)
        {
            // Act
            var result = IfoodLogisticsDelivery.Create(1, 1, invalidName!, "11999998888", "MOTORCYCLE", Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.MissingDriverName");
            result.Error.Message.Should().Be("Driver name is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceDriverPhone_ShouldReturnFailureResult(string? invalidPhone)
        {
            // Act
            var result = IfoodLogisticsDelivery.Create(1, 1, "Joao Silva", invalidPhone!, "MOTORCYCLE", Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.MissingDriverPhone");
            result.Error.Message.Should().Be("Driver phone is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceVehicleType_ShouldReturnFailureResult(string? invalidVehicleType)
        {
            // Act
            var result = IfoodLogisticsDelivery.Create(1, 1, "Joao Silva", "11999998888", invalidVehicleType!, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.MissingVehicleType");
            result.Error.Message.Should().Be("Driver vehicle type is required.");
        }

        [Fact]
        public void MarkGoingToOrigin_WhenDriverAssigned_ShouldReturnSuccessAndAdvanceStatus()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            var transitionTime = Now.AddMinutes(5);

            // Act
            var result = delivery.MarkGoingToOrigin(transitionTime);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodLogisticsStatuses.GoingToOrigin);
            delivery.GoingToOriginAt.Should().Be(transitionTime);
            delivery.UpdatedAt.Should().Be(transitionTime);
        }

        [Fact]
        public void MarkGoingToOrigin_WhenNotDriverAssigned_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            delivery.MarkGoingToOrigin(Now.AddMinutes(5)); // status is now GoingToOrigin

            // Act
            var result = delivery.MarkGoingToOrigin(Now.AddMinutes(6));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        }

        [Fact]
        public void MarkArrivedAtOrigin_WhenGoingToOrigin_ShouldReturnSuccessAndAdvanceStatus()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            delivery.MarkGoingToOrigin(Now.AddMinutes(5));
            var transitionTime = Now.AddMinutes(10);

            // Act
            var result = delivery.MarkArrivedAtOrigin(transitionTime);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodLogisticsStatuses.ArrivedAtOrigin);
            delivery.ArrivedAtOriginAt.Should().Be(transitionTime);
            delivery.UpdatedAt.Should().Be(transitionTime);
        }

        [Fact]
        public void MarkArrivedAtOrigin_WhenNotGoingToOrigin_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValidDelivery(); // still DriverAssigned

            // Act
            var result = delivery.MarkArrivedAtOrigin(Now.AddMinutes(5));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        }

        [Fact]
        public void MarkDispatched_WhenArrivedAtOrigin_ShouldReturnSuccessAndAdvanceStatus()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            delivery.MarkGoingToOrigin(Now.AddMinutes(5));
            delivery.MarkArrivedAtOrigin(Now.AddMinutes(10));
            var transitionTime = Now.AddMinutes(15);

            // Act
            var result = delivery.MarkDispatched(transitionTime);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodLogisticsStatuses.Dispatched);
            delivery.DispatchedAt.Should().Be(transitionTime);
            delivery.UpdatedAt.Should().Be(transitionTime);
        }

        [Fact]
        public void MarkDispatched_WhenNotArrivedAtOrigin_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValidDelivery(); // still DriverAssigned

            // Act
            var result = delivery.MarkDispatched(Now.AddMinutes(5));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        }

        [Fact]
        public void MarkArrivedAtDestination_WhenDispatched_ShouldReturnSuccessAndAdvanceStatus()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            delivery.MarkGoingToOrigin(Now.AddMinutes(5));
            delivery.MarkArrivedAtOrigin(Now.AddMinutes(10));
            delivery.MarkDispatched(Now.AddMinutes(15));
            var transitionTime = Now.AddMinutes(25);

            // Act
            var result = delivery.MarkArrivedAtDestination(transitionTime);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodLogisticsStatuses.ArrivedAtDestination);
            delivery.ArrivedAtDestinationAt.Should().Be(transitionTime);
            delivery.UpdatedAt.Should().Be(transitionTime);
        }

        [Fact]
        public void MarkArrivedAtDestination_WhenNotDispatched_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValidDelivery(); // still DriverAssigned

            // Act
            var result = delivery.MarkArrivedAtDestination(Now.AddMinutes(5));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        }

        [Fact]
        public void MarkDeliveryCodeVerified_WhenArrivedAtDestination_ShouldReturnSuccessAndAdvanceStatus()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            delivery.MarkGoingToOrigin(Now.AddMinutes(5));
            delivery.MarkArrivedAtOrigin(Now.AddMinutes(10));
            delivery.MarkDispatched(Now.AddMinutes(15));
            delivery.MarkArrivedAtDestination(Now.AddMinutes(25));
            var transitionTime = Now.AddMinutes(30);

            // Act
            var result = delivery.MarkDeliveryCodeVerified(transitionTime);

            // Assert
            result.IsSuccess.Should().BeTrue();
            delivery.Status.Should().Be(IfoodLogisticsStatuses.DeliveryCodeVerified);
            delivery.DeliveryCodeVerifiedAt.Should().Be(transitionTime);
            delivery.UpdatedAt.Should().Be(transitionTime);
        }

        [Fact]
        public void MarkDeliveryCodeVerified_WhenNotArrivedAtDestination_ShouldReturnFailureResult()
        {
            // Arrange
            var delivery = CreateValidDelivery(); // still DriverAssigned

            // Act
            var result = delivery.MarkDeliveryCodeVerified(Now.AddMinutes(5));

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodLogisticsDelivery.InvalidTransition");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var delivery = CreateValidDelivery();
            var deactivationTime = Now.AddHours(1);

            // Act
            delivery.Deactivate(deactivationTime);

            // Assert
            delivery.IsActive.Should().BeFalse();
            delivery.UpdatedAt.Should().Be(deactivationTime);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodLogisticsDelivery), true) as IfoodLogisticsDelivery;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
