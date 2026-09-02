using FluentAssertions;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class TableItemTransferTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long customerOrderId = 1;
            long customerOrderItemId = 2;
            long sourceDiningTableId = 3;
            long targetDiningTableId = 4;
            long employeeId = 5;

            // Act
            var result = TableItemTransfer.Create(customerOrderId, customerOrderItemId, sourceDiningTableId, targetDiningTableId, employeeId);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var transfer = result.Value;
            transfer.Should().NotBeNull();
            transfer.CustomerOrderId.Should().Be(customerOrderId);
            transfer.CustomerOrderItemId.Should().Be(customerOrderItemId);
            transfer.SourceDiningTableId.Should().Be(sourceDiningTableId);
            transfer.TargetDiningTableId.Should().Be(targetDiningTableId);
            transfer.EmployeeId.Should().Be(employeeId);
            transfer.IsActive.Should().BeTrue();
            transfer.CreatedAt.Should().BeCloseTo(DateTime.Now, TimeSpan.FromSeconds(1));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidCustomerOrderId_ShouldReturnFailureResult(long invalidOrderId)
        {
            // Act
            var result = TableItemTransfer.Create(invalidOrderId, 2, 3, 4, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableItemTransfer.InvalidOrder");
            result.Error.Message.Should().Be("Order ID must be valid.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidCustomerOrderItemId_ShouldReturnFailureResult(long invalidItemId)
        {
            // Act
            var result = TableItemTransfer.Create(1, invalidItemId, 3, 4, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableItemTransfer.InvalidItem");
            result.Error.Message.Should().Be("Item ID must be valid.");
        }

        [Fact]
        public void Create_WithSameSourceAndTargetTable_ShouldReturnFailureResult()
        {
            // Act
            var result = TableItemTransfer.Create(1, 2, 3, 3, 5);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableItemTransfer.SameTable");
            result.Error.Message.Should().Be("Source and target tables cannot be the same.");
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidEmployeeId_ShouldReturnFailureResult(long invalidEmployeeId)
        {
            // Act
            var result = TableItemTransfer.Create(1, 2, 3, 4, invalidEmployeeId);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("TableItemTransfer.InvalidEmployee");
            result.Error.Message.Should().Be("Employee ID must be valid.");
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(TableItemTransfer), true) as TableItemTransfer;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
