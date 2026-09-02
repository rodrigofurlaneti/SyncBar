using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class ComandaItemTransferTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long customerOrderId = 1;
            long customerOrderItemId = 2;
            long sourceComandaId = 10;
            long targetComandaId = 20;
            long employeeId = 5;

            // Act
            var result = ComandaItemTransfer.Create(customerOrderId, customerOrderItemId, sourceComandaId, targetComandaId, employeeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var transfer = result.Value;
            transfer.Should().NotBeNull();
            transfer.CustomerOrderId.Should().Be(customerOrderId);
            transfer.CustomerOrderItemId.Should().Be(customerOrderItemId);
            transfer.SourceComandaId.Should().Be(sourceComandaId);
            transfer.TargetComandaId.Should().Be(targetComandaId);
            transfer.EmployeeId.Should().Be(employeeId);
            transfer.IsActive.Should().BeTrue();
            transfer.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidCustomerOrderId_ShouldReturnFailureResult(long invalidCustomerOrderId)
        {
            // Act
            var result = ComandaItemTransfer.Create(invalidCustomerOrderId, 2, 10, 20, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaItemTransfer.InvalidOrder");
            result.Error.Message.Should().Be("Order ID must be valid.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidCustomerOrderItemId_ShouldReturnFailureResult(long invalidItemId)
        {
            // Act
            var result = ComandaItemTransfer.Create(1, invalidItemId, 10, 20, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaItemTransfer.InvalidItem");
            result.Error.Message.Should().Be("Item ID must be valid.");
        }

        [Fact]
        public void Create_WithSameSourceAndTargetComanda_ShouldReturnFailureResult()
        {
            // Act
            var result = ComandaItemTransfer.Create(1, 2, 10, 10, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaItemTransfer.SameComanda");
            result.Error.Message.Should().Be("Source and target comandas cannot be the same.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidEmployeeId_ShouldReturnFailureResult(long invalidEmployeeId)
        {
            // Act
            var result = ComandaItemTransfer.Create(1, 2, 10, 20, invalidEmployeeId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("ComandaItemTransfer.InvalidEmployee");
            result.Error.Message.Should().Be("Employee ID must be valid.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(ComandaItemTransfer), true) as ComandaItemTransfer;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
