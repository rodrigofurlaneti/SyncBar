using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class IfoodOrderTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Arrange
            long customerOrderId = 1;
            long branchId = 2;
            string ifoodOrderId = "abc-123";
            string displayId = "#001";
            string merchantId = "merchant-1";
            string ifoodOrderType = "DELIVERY";
            string deliveredBy = "Ifood";
            string orderTiming = "IMMEDIATE";
            DateTime? preparationStartDateTime = null;
            var now = new DateTime(2026, 9, 2, 12, 0, 0);
            bool hasUnmappedItems = false;

            // Act
            var result = IfoodOrder.Create(customerOrderId, branchId, ifoodOrderId, displayId, merchantId,
                ifoodOrderType, deliveredBy, orderTiming, preparationStartDateTime, now, hasUnmappedItems);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var order = result.Value;
            order.Should().NotBeNull();
            order.CustomerOrderId.Should().Be(customerOrderId);
            order.BranchId.Should().Be(branchId);
            order.IfoodOrderId.Should().Be(ifoodOrderId);
            order.DisplayId.Should().Be(displayId);
            order.MerchantId.Should().Be(merchantId);
            order.IfoodOrderType.Should().Be(ifoodOrderType);
            order.DeliveredBy.Should().Be(deliveredBy);
            order.OrderTiming.Should().Be(orderTiming);
            order.PreparationStartDateTime.Should().BeNull();
            order.Status.Should().Be(IfoodOrderStatuses.Placed);
            order.ConfirmDeadlineAt.Should().Be(now.AddMinutes(8));
            order.ConfirmedAt.Should().BeNull();
            order.HasUnmappedItems.Should().Be(hasUnmappedItems);
            order.IsActive.Should().BeTrue();
            order.CreatedAt.Should().Be(now);
            order.UpdatedAt.Should().BeNull();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithBlankOrderTiming_ShouldDefaultToImmediate(string? blankTiming)
        {
            // Act
            var result = IfoodOrder.Create(1, 2, "abc-123", "#001", "merchant-1", "DELIVERY", "Ifood",
                blankTiming, null, DateTime.Now, false);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.OrderTiming.Should().Be("IMMEDIATE");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceIfoodOrderId_ShouldReturnFailureResult(string? invalidId)
        {
            // Act
            var result = IfoodOrder.Create(1, 2, invalidId, "#001", "merchant-1", "DELIVERY", "Ifood",
                "IMMEDIATE", null, DateTime.Now, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOrder.MissingId");
            result.Error.Message.Should().Be("Ifood order id is required.");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void Create_WithEmptyOrWhitespaceMerchantId_ShouldReturnFailureResult(string? invalidMerchantId)
        {
            // Act
            var result = IfoodOrder.Create(1, 2, "abc-123", "#001", invalidMerchantId, "DELIVERY", "Ifood",
                "IMMEDIATE", null, DateTime.Now, false);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("IfoodOrder.MissingMerchantId");
            result.Error.Message.Should().Be("Merchant id is required.");
        }

        [Fact]
        public void MarkConfirmed_ShouldSetStatusConfirmedAndTimestamps()
        {
            // Arrange
            var order = IfoodOrder.Create(1, 2, "abc-123", "#001", "merchant-1", "DELIVERY", "Ifood",
                "IMMEDIATE", null, DateTime.Now, false).Value;
            var confirmedAt = DateTime.Now.AddMinutes(2);

            // Act
            order.MarkConfirmed(confirmedAt);

            // Assert
            order.Status.Should().Be(IfoodOrderStatuses.Confirmed);
            order.ConfirmedAt.Should().Be(confirmedAt);
            order.UpdatedAt.Should().Be(confirmedAt);
        }

        [Fact]
        public void SetStatus_ShouldUpdateStatusAndUpdatedAt()
        {
            // Arrange
            var order = IfoodOrder.Create(1, 2, "abc-123", "#001", "merchant-1", "DELIVERY", "Ifood",
                "IMMEDIATE", null, DateTime.Now, false).Value;
            var now = DateTime.Now.AddMinutes(5);

            // Act
            order.SetStatus(IfoodOrderStatuses.Dispatched, now);

            // Assert
            order.Status.Should().Be(IfoodOrderStatuses.Dispatched);
            order.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void Deactivate_ShouldSetIsActiveFalseAndUpdatedAt()
        {
            // Arrange
            var order = IfoodOrder.Create(1, 2, "abc-123", "#001", "merchant-1", "DELIVERY", "Ifood",
                "IMMEDIATE", null, DateTime.Now, false).Value;
            var now = DateTime.Now.AddMinutes(1);

            // Act
            order.Deactivate(now);

            // Assert
            order.IsActive.Should().BeFalse();
            order.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(IfoodOrder), true) as IfoodOrder;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
