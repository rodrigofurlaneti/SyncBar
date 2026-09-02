using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class OrderItemTests
    {
        [Fact]
        public void Create_WithValidArguments_ShouldReturnSuccessResultWithCorrectCalculations()
        {
            // Arrange
            long customerOrderId = 1;
            long productId = 2;
            decimal unitPrice = 10.50m;
            decimal quantity = 3;
            string notes = "Sem cebola";
            long? employeeId = 5;
            var now = DateTime.Now;

            // Act
            var result = OrderItem.Create(customerOrderId, productId, unitPrice, quantity, notes, employeeId, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var item = result.Value;
            item.Should().NotBeNull();
            item.CustomerOrderId.Should().Be(customerOrderId);
            item.ProductId.Should().Be(productId);
            item.UnitPrice.Should().Be(unitPrice);
            item.Quantity.Should().Be(quantity);
            item.Notes.Should().Be(notes);
            item.EmployeeId.Should().Be(employeeId);
            item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Lancado);
            // Total = 10.50 * 3 = 31.50
            item.TotalAmount.Should().Be(31.50m);
            item.IsActive.Should().BeTrue();
            item.CreatedAt.Should().Be(now);
            item.UpdatedAt.Should().BeNull();
            item.Complements.Should().BeEmpty();
            item.PizzaFlavors.Should().BeEmpty();
            item.PizzaSizeId.Should().BeNull();
            item.PizzaCrustId.Should().BeNull();
            item.PizzaEdgeId.Should().BeNull();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void Create_WithInvalidQuantity_ShouldReturnFailureResult(decimal invalidQuantity)
        {
            // Act
            var result = OrderItem.Create(1, 2, 10m, invalidQuantity, null, null, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.InvalidQuantity");
            result.Error.Message.Should().Be("Quantity must be greater than zero.");
        }

        [Fact]
        public void Create_WithNegativeUnitPrice_ShouldReturnFailureResult()
        {
            // Act
            var result = OrderItem.Create(1, 2, -1m, 1, null, null, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.InvalidUnitPrice");
            result.Error.Message.Should().Be("Unit price cannot be negative.");
        }

        [Fact]
        public void CreatePizza_WithValidArguments_ShouldReturnSuccessResultWithFlavorsSplitEvenly()
        {
            // Arrange
            var now = DateTime.Now;
            long pizzaSizeId = 100;
            long pizzaCrustId = 200;
            long pizzaEdgeId = 300;
            var flavorIds = new List<long> { 1, 2 };

            // Act
            var result = OrderItem.CreatePizza(1, 2, 40m, 1, "Meio a meio", 5, now, pizzaSizeId, pizzaCrustId, pizzaEdgeId, flavorIds);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var item = result.Value;
            item.PizzaSizeId.Should().Be(pizzaSizeId);
            item.PizzaCrustId.Should().Be(pizzaCrustId);
            item.PizzaEdgeId.Should().Be(pizzaEdgeId);
            item.PizzaFlavors.Should().HaveCount(2);
            item.PizzaFlavors.Should().OnlyContain(f => f.FractionShare == 0.5m);
            item.PizzaFlavors.Select(f => f.PizzaFlavorId).Should().BeEquivalentTo(flavorIds);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void CreatePizza_WithInvalidQuantity_ShouldReturnFailureResult(decimal invalidQuantity)
        {
            // Act
            var result = OrderItem.CreatePizza(1, 2, 40m, invalidQuantity, null, null, DateTime.Now, 100, null, null, new List<long> { 1 });

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.InvalidQuantity");
        }

        [Fact]
        public void CreatePizza_WithNegativeUnitPrice_ShouldReturnFailureResult()
        {
            // Act
            var result = OrderItem.CreatePizza(1, 2, -1m, 1, null, null, DateTime.Now, 100, null, null, new List<long> { 1 });

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.InvalidUnitPrice");
        }

        [Fact]
        public void CreatePizza_WithNoFlavorsSelected_ShouldReturnFailureResult()
        {
            // Act
            var result = OrderItem.CreatePizza(1, 2, 40m, 1, null, null, DateTime.Now, 100, null, null, Array.Empty<long>());

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.NoFlavorsSelected");
            result.Error.Message.Should().Be("At least one pizza flavor must be selected.");
        }

        [Fact]
        public void ForceCancelForTransfer_ShouldSetCancelledStatusAndActor()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            long actorEmployeeId = 9;
            var now = DateTime.Now;

            // Act
            var result = item.ForceCancelForTransfer(actorEmployeeId, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
            item.CancelledByEmployeeId.Should().Be(actorEmployeeId);
            item.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void UpdateStatus_ToEnviadoCozinha_ShouldSetSentToKitchenAt()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            var now = DateTime.Now;

            // Act
            var result = item.UpdateStatus(OrderItemStatusIds.EnviadoCozinha, null, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.OrderItemStatusId.Should().Be(OrderItemStatusIds.EnviadoCozinha);
            item.SentToKitchenAt.Should().Be(now);
            item.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void UpdateStatus_ToEntregue_ShouldSetDeliveredAt()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            var now = DateTime.Now;

            // Act
            var result = item.UpdateStatus(OrderItemStatusIds.Entregue, null, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Entregue);
            item.DeliveredAt.Should().Be(now);
        }

        [Fact]
        public void UpdateStatus_ToCancelado_ShouldSetCancelledByEmployeeId()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            long actorEmployeeId = 7;
            var now = DateTime.Now;

            // Act
            var result = item.UpdateStatus(OrderItemStatusIds.Cancelado, actorEmployeeId, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
            item.CancelledByEmployeeId.Should().Be(actorEmployeeId);
        }

        [Fact]
        public void UpdateStatus_WhenItemAlreadyDelivered_ShouldReturnFailureResult()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            item.UpdateStatus(OrderItemStatusIds.Entregue, null, DateTime.Now);

            // Act
            var result = item.UpdateStatus(OrderItemStatusIds.Pronto, null, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.FinalStatus");
            result.Error.Message.Should().Be("Delivered or cancelled items cannot change status.");
        }

        [Fact]
        public void UpdateStatus_WhenItemAlreadyCancelled_ShouldReturnFailureResult()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            item.UpdateStatus(OrderItemStatusIds.Cancelado, null, DateTime.Now);

            // Act
            var result = item.UpdateStatus(OrderItemStatusIds.Pronto, null, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.FinalStatus");
        }

        [Fact]
        public void AddComplement_WithValidArguments_ShouldAddComplementAndRecalculateTotal()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 2, null, null, DateTime.Now).Value; // Base total = 20
            var now = DateTime.Now;

            // Act
            var result = item.AddComplement(50, 5m, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Complements.Should().HaveCount(1);
            item.Complements.First().ComplementId.Should().Be(50);
            item.Complements.First().UnitPriceCharged.Should().Be(5m);
            // Total = (10 * 2) + 5 = 25
            item.TotalAmount.Should().Be(25m);
            item.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void AddComplement_WithNegativePrice_ShouldReturnFailureResultAndNotAddComplement()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;

            // Act
            var result = item.AddComplement(50, -1m, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItemComplement.InvalidPrice");
            item.Complements.Should().BeEmpty();
            item.TotalAmount.Should().Be(10m);
        }

        [Fact]
        public void AddComplement_WhenItemIsInFinalStatus_ShouldReturnFailureResult()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            item.UpdateStatus(OrderItemStatusIds.Cancelado, null, DateTime.Now);

            // Act
            var result = item.AddComplement(50, 5m, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.FinalStatus");
            item.Complements.Should().BeEmpty();
        }

        [Fact]
        public void RemoveComplement_WithExistingComplement_ShouldDeactivateItAndRecalculateTotal()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value; // Base total = 10
            item.AddComplement(50, 5m, DateTime.Now); // Total becomes 15
            var complementId = item.Complements.First().Id;
            var now = DateTime.Now;

            // Act
            var result = item.RemoveComplement(complementId, now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            item.Complements.First().IsActive.Should().BeFalse();
            item.TotalAmount.Should().Be(10m);
            item.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void RemoveComplement_WhenComplementDoesNotExist_ShouldReturnFailureResult()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;

            // Act
            var result = item.RemoveComplement(999, DateTime.Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.ComplementNotFound");
            result.Error.Message.Should().Be("Order item complement not found.");
        }

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var item = OrderItem.Create(1, 2, 10m, 1, null, null, DateTime.Now).Value;
            var now = DateTime.Now;

            // Act
            item.Deactivate(now);

            // Assert
            item.IsActive.Should().BeFalse();
            item.UpdatedAt.Should().Be(now);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(OrderItem), true) as OrderItem;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
