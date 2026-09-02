using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain.Entities
{
    public class CustomerOrderTests
    {
        private static readonly DateTime Now = new(2026, 9, 2, 10, 0, 0);

        // Every CustomerOrder created via .Create(...) in these tests has Id == 0 (protected
        // setter). Order items added to it also always get Id == 0 (OrderItem's own ctor calls
        // base(0)) — since each test only ever has a single active item at a time, looking it
        // up by Id (as CustomerOrder's own methods do, e.g. AddComplement(orderItemId, ...))
        // is unambiguous and needs no reflection workaround.
        private static CustomerOrder CreateOpenMesaOrder(long? diningTableId = 1, long? comandaId = null, decimal? creditLimitAmount = null)
            => CustomerOrder.Create(1, diningTableId, comandaId, 10, 4, null, Now, creditLimitAmount).Value;

        private static CustomerOrder CreateOpenComandaOrder(decimal creditLimitAmount = 100m)
            => CustomerOrder.Create(1, null, 7, 10, null, null, Now, creditLimitAmount).Value;

        private static Product CreateProduct(decimal salePrice = 10.0m)
            => Product.Create(1, 1, 1, "Coca-Cola", null, null, salePrice, 5.0m, false, null).Value;

        // ---------- Create ----------

        [Fact]
        public void Create_WithDiningTable_ShouldReturnSuccessResultWithCorrectProperties()
        {
            // Act
            var result = CustomerOrder.Create(1, 5, null, 10, 4, "Sem cebola", Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var order = result.Value;
            order.Should().NotBeNull();
            order.BranchId.Should().Be(1);
            order.DiningTableId.Should().Be(5);
            order.ComandaId.Should().BeNull();
            order.EmployeeId.Should().Be(10);
            order.GuestCount.Should().Be(4);
            order.Notes.Should().Be("Sem cebola");
            order.OrderTypeId.Should().Be(OrderTypeIds.Mesa);
            order.OrderStatusId.Should().Be(OrderStatusIds.Aberto);
            order.OpenedAt.Should().Be(Now);
            order.ClosedAt.Should().BeNull();
            order.SubtotalAmount.Should().Be(0m);
            order.DiscountAmount.Should().Be(0m);
            order.ServiceFeeAmount.Should().Be(0m);
            order.TotalAmount.Should().Be(0m);
            order.IsActive.Should().BeTrue();
            order.Items.Should().BeEmpty();
            order.CreatedAt.Should().Be(Now);
            order.UpdatedAt.Should().BeNull();
        }

        [Fact]
        public void Create_WithComandaId_ShouldKeepCreditLimitAmount()
        {
            // Act
            var result = CustomerOrder.Create(1, null, 7, 10, null, null, Now, creditLimitAmount: 150m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.ComandaId.Should().Be(7);
            result.Value.CreditLimitAmount.Should().Be(150m);
        }

        [Fact]
        public void Create_WithDiningTableAndCreditLimit_ShouldDiscardCreditLimitBecauseNoComanda()
        {
            // Act — CreditLimitAmount only survives when comandaId is not null.
            var result = CustomerOrder.Create(1, 5, null, 10, 4, null, Now, creditLimitAmount: 150m);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.CreditLimitAmount.Should().BeNull();
        }

        [Fact]
        public void Create_ForMesaWithoutTableOrComanda_ShouldReturnFailureResult()
        {
            // Act
            var result = CustomerOrder.Create(1, null, null, 10, 4, null, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.MissingOrigin");
            result.Error.Message.Should().Be("Order must have a dining table or a comanda.");
        }

        [Fact]
        public void Create_ForTakeawayWithoutCustomerName_ShouldReturnFailureResult()
        {
            // Act
            var result = CustomerOrder.Create(1, null, null, 10, null, null, Now, orderTypeId: OrderTypeIds.Retirada);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.MissingCustomerName");
            result.Error.Message.Should().Be("Takeaway/delivery orders require a customer name.");
        }

        [Fact]
        public void Create_ForDeliveryWithoutDeliveryAddress_ShouldReturnFailureResult()
        {
            // Act
            var result = CustomerOrder.Create(1, null, null, 10, null, null, Now,
                orderTypeId: OrderTypeIds.Delivery, customerName: "Maria");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.MissingDeliveryAddress");
            result.Error.Message.Should().Be("Delivery orders require a delivery address.");
        }

        [Fact]
        public void Create_ForDeliveryWithCustomerNameAndAddress_ShouldReturnSuccessResult()
        {
            // Act
            var result = CustomerOrder.Create(1, null, null, 10, null, null, Now,
                orderTypeId: OrderTypeIds.Delivery, customerName: "Maria", deliveryAddress: "Rua A, 123");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Value.CustomerName.Should().Be("Maria");
            result.Value.DeliveryAddress.Should().Be("Rua A, 123");
        }

        // ---------- AddItem ----------

        [Fact]
        public void AddItem_WithValidArguments_ShouldAddItemAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddItem(100, 10.0m, 2m, "Sem gelo", 10, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(1);
            var item = order.Items.First();
            item.ProductId.Should().Be(100);
            item.UnitPrice.Should().Be(10.0m);
            item.Quantity.Should().Be(2m);
            item.TotalAmount.Should().Be(20.0m);

            order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
            order.SubtotalAmount.Should().Be(20.0m);
            order.TotalAmount.Should().Be(20.0m);
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void AddItem_WithEmployeeIdZero_ShouldStoreNullEmployeeId()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddItem(100, 10.0m, 1m, null, 0, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.First().EmployeeId.Should().BeNull();
        }

        [Fact]
        public void AddItem_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.Cancel(Now);

            // Act
            var result = order.AddItem(100, 10.0m, 1m, null, 10, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
            result.Error.Message.Should().Be("Items can only be added to an open order.");
        }

        [Fact]
        public void AddItem_WithNonPositiveQuantity_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddItem(100, 10.0m, 0m, null, 10, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.InvalidQuantity");
            result.Error.Message.Should().Be("Quantity must be greater than zero.");
        }

        [Fact]
        public void AddItem_WhenProspectiveTotalExceedsCreditLimit_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenComandaOrder(creditLimitAmount: 15m);

            // Act
            var result = order.AddItem(100, 10.0m, 2m, null, 10, Now); // 20.00 > 15.00

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Comanda.LimitExceeded");
            order.Items.Should().BeEmpty();
        }

        [Fact]
        public void AddItem_WhenProspectiveTotalIsWithinCreditLimit_ShouldSucceed()
        {
            // Arrange
            var order = CreateOpenComandaOrder(creditLimitAmount: 25m);

            // Act
            var result = order.AddItem(100, 10.0m, 2m, null, 10, Now); // 20.00 <= 25.00

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(1);
        }

        [Fact]
        public void AddItem_WithNegativeUnitPrice_ShouldPropagateOrderItemFailure()
        {
            // Arrange — CustomerOrder.AddItem itself does not validate unit price; the failure
            // is propagated from OrderItem.Create's own validation.
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddItem(100, -1.0m, 1m, null, 10, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.InvalidUnitPrice");
            order.Items.Should().BeEmpty();
        }

        // ---------- AddPizzaItem ----------

        [Fact]
        public void AddPizzaItem_WithValidArguments_ShouldAddItemAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddPizzaItem(100, 40.0m, 1m, null, 10, Now, pizzaSizeId: 1, pizzaCrustId: 2, pizzaEdgeId: 3, pizzaFlavorIds: [11, 12]);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(1);
            var item = order.Items.First();
            item.PizzaSizeId.Should().Be(1);
            item.PizzaCrustId.Should().Be(2);
            item.PizzaEdgeId.Should().Be(3);
            item.PizzaFlavors.Should().HaveCount(2);

            order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
            order.SubtotalAmount.Should().Be(40.0m);
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void AddPizzaItem_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.Cancel(Now);

            // Act
            var result = order.AddPizzaItem(100, 40.0m, 1m, null, 10, Now, 1, null, null, [11]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void AddPizzaItem_WithNonPositiveQuantity_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddPizzaItem(100, 40.0m, 0m, null, 10, Now, 1, null, null, [11]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.InvalidQuantity");
        }

        [Fact]
        public void AddPizzaItem_WhenProspectiveTotalExceedsCreditLimit_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenComandaOrder(creditLimitAmount: 15m);

            // Act
            var result = order.AddPizzaItem(100, 40.0m, 1m, null, 10, Now, 1, null, null, [11]);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Comanda.LimitExceeded");
        }

        [Fact]
        public void AddPizzaItem_WithNoFlavorsSelected_ShouldPropagateOrderItemFailure()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.AddPizzaItem(100, 40.0m, 1m, null, 10, Now, 1, null, null, []);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.NoFlavorsSelected");
            order.Items.Should().BeEmpty();
        }

        // ---------- AddItemWithPromotion ----------

        [Fact]
        public void AddItemWithPromotion_WithoutPromotion_ShouldAddSingleItemAtRegularPrice()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            var product = CreateProduct(salePrice: 10.0m);

            // Act
            var result = order.AddItemWithPromotion(product, 2m, null, null, 10, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(1);
            order.Items.First().UnitPrice.Should().Be(10.0m);
        }

        [Fact]
        public void AddItemWithPromotion_WithDescontoPromotion_ShouldApplyDiscountedPriceAndTagNotes()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            var product = CreateProduct(salePrice: 10.0m);
            var promotion = Promotion.Create(1, product.Id, "Happy Hour", (int)Now.DayOfWeek, 0, 1440,
                PromotionTypeIds.Desconto, 0.5m).Value;

            // Act
            var result = order.AddItemWithPromotion(product, 1m, null, promotion, 10, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(1);
            var item = order.Items.First();
            item.UnitPrice.Should().Be(5.0m); // 10 * (1 - 0.5)
            item.Notes.Should().Contain("Happy Hour");
        }

        [Fact]
        public void AddItemWithPromotion_WithEmDobroPromotion_ShouldAddBonusItemAtZeroPrice()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            var product = CreateProduct(salePrice: 10.0m);
            var promotion = Promotion.Create(1, product.Id, "Dose Dupla", (int)Now.DayOfWeek, 0, 1440,
                PromotionTypeIds.EmDobro).Value;

            // Act
            var result = order.AddItemWithPromotion(product, 1m, null, promotion, 10, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.Should().HaveCount(2);
            order.Items.First().UnitPrice.Should().Be(10.0m);
            var bonusItem = order.Items.Last();
            bonusItem.UnitPrice.Should().Be(0m);
            bonusItem.Notes.Should().Contain("Dose Dupla");
        }

        [Fact]
        public void AddItemWithPromotion_WhenOrderIsNotOpen_ShouldPropagateFailure()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.Cancel(Now);
            var product = CreateProduct();

            // Act
            var result = order.AddItemWithPromotion(product, 1m, null, null, 10, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        // ---------- AddComplement / RemoveComplement ----------

        [Fact]
        public void AddComplement_WithValidArguments_ShouldAddComplementAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;

            // Act
            var result = order.AddComplement(orderItemId, 50, 2.0m, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.First().Complements.Should().HaveCount(1);
            order.SubtotalAmount.Should().Be(12.0m); // 10 + 2
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void AddComplement_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.Cancel(Now);

            // Act
            var result = order.AddComplement(orderItemId, 50, 2.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void AddComplement_WhenItemNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);

            // Act
            var result = order.AddComplement(999, 50, 2.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
            result.Error.Message.Should().Be("Order item not found.");
        }

        [Fact]
        public void AddComplement_WhenItemHasFinalStatus_ShouldPropagateOrderItemFailure()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Entregue, Now);

            // Act
            var result = order.AddComplement(orderItemId, 50, 2.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.FinalStatus");
        }

        [Fact]
        public void RemoveComplement_WithValidArguments_ShouldRemoveComplementAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.AddComplement(orderItemId, 50, 2.0m, Now);
            var complementId = order.Items.First().Complements.First().Id;

            // Act
            var result = order.RemoveComplement(orderItemId, complementId, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.SubtotalAmount.Should().Be(10.0m);
        }

        [Fact]
        public void RemoveComplement_WhenComplementNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;

            // Act
            var result = order.RemoveComplement(orderItemId, 999, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.ComplementNotFound");
        }

        [Fact]
        public void RemoveComplement_WhenItemNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.RemoveComplement(999, 1, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
        }

        [Fact]
        public void RemoveComplement_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.AddComplement(orderItemId, 50, 2.0m, Now);
            var complementId = order.Items.First().Complements.First().Id;
            order.Cancel(Now);

            // Act
            var result = order.RemoveComplement(orderItemId, complementId, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        // ---------- UpdateItemStatus ----------

        [Fact]
        public void UpdateItemStatus_WithValidArguments_ShouldUpdateStatusAndUpdatedAt()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;

            // Act
            var result = order.UpdateItemStatus(orderItemId, OrderItemStatusIds.EnviadoCozinha, Now, actorEmployeeId: 10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.First().OrderItemStatusId.Should().Be(OrderItemStatusIds.EnviadoCozinha);
            order.Items.First().SentToKitchenAt.Should().Be(Now);
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void UpdateItemStatus_ToCancelado_ShouldRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;

            // Act
            var result = order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Cancelado, Now, actorEmployeeId: 10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.SubtotalAmount.Should().Be(0m);
            order.TotalAmount.Should().Be(0m);
        }

        [Fact]
        public void UpdateItemStatus_WhenItemNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.UpdateItemStatus(999, OrderItemStatusIds.Pronto, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
        }

        [Fact]
        public void UpdateItemStatus_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.Cancel(Now);

            // Act
            var result = order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Pronto, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void UpdateItemStatus_WhenItemAlreadyInFinalStatus_ShouldPropagateOrderItemFailure()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Entregue, Now);

            // Act
            var result = order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Pronto, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("OrderItem.FinalStatus");
        }

        // ---------- ForceCancelItemForTransfer ----------

        [Fact]
        public void ForceCancelItemForTransfer_WithValidArguments_ShouldCancelItemAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;

            // Act
            var result = order.ForceCancelItemForTransfer(orderItemId, Now, actorEmployeeId: 20);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.Items.First().OrderItemStatusId.Should().Be(OrderItemStatusIds.Cancelado);
            order.Items.First().CancelledByEmployeeId.Should().Be(20);
            order.SubtotalAmount.Should().Be(0m);
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void ForceCancelItemForTransfer_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.Cancel(Now);

            // Act
            var result = order.ForceCancelItemForTransfer(orderItemId, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void ForceCancelItemForTransfer_WhenItemNotFound_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.ForceCancelItemForTransfer(999, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.ItemNotFound");
        }

        // ---------- ApplyDiscount ----------

        [Fact]
        public void ApplyDiscount_WithValidArguments_ShouldSetDiscountAndRecalculateTotals()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 2m, null, 10, Now); // Subtotal = 20

            // Act
            var result = order.ApplyDiscount(5.0m, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.DiscountAmount.Should().Be(5.0m);
            order.TotalAmount.Should().Be(15.0m);
        }

        [Fact]
        public void ApplyDiscount_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.Cancel(Now);

            // Act
            var result = order.ApplyDiscount(1.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void ApplyDiscount_WithNegativeAmount_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.ApplyDiscount(-1.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.InvalidDiscount");
            result.Error.Message.Should().Be("Discount cannot be negative.");
        }

        [Fact]
        public void ApplyDiscount_ExceedingSubtotal_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now); // Subtotal = 10

            // Act
            var result = order.ApplyDiscount(11.0m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.DiscountExceedsSubtotal");
            result.Error.Message.Should().Be("Discount cannot exceed the subtotal.");
        }

        // ---------- Close ----------

        [Fact]
        public void Close_WithValidArguments_ShouldComputeServiceFeeAndAwaitPayment()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 10m, null, 10, Now); // Subtotal = 100

            // Act
            var result = order.Close(0.10m, Now); // 10% service fee

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.ServiceFeeAmount.Should().Be(10.0m); // (100 - 0) * 0.10
            order.TotalAmount.Should().Be(110.0m);
            order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
        }

        [Fact]
        public void Close_WhenOrderIsNotOpen_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            order.Cancel(Now);

            // Act
            var result = order.Close(0.10m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotOpen");
        }

        [Fact]
        public void Close_WithNoActiveItems_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.Close(0.10m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NoItems");
            result.Error.Message.Should().Be("Order has no items to close.");
        }

        [Fact]
        public void Close_WhenAllItemsAreCancelled_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            var orderItemId = order.Items.First().Id;
            order.UpdateItemStatus(orderItemId, OrderItemStatusIds.Cancelado, Now);

            // Act
            var result = order.Close(0.10m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NoItems");
        }

        [Fact]
        public void Close_WithNegativeServiceFeeRate_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);

            // Act
            var result = order.Close(-0.1m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.InvalidServiceFee");
            result.Error.Message.Should().Be("Service fee rate cannot be negative.");
        }

        // ---------- RaiseCreditLimit ----------

        [Fact]
        public void RaiseCreditLimit_WithValidArguments_ShouldUpdateCreditLimit()
        {
            // Arrange
            var order = CreateOpenComandaOrder(creditLimitAmount: 100m);

            // Act
            var result = order.RaiseCreditLimit(200m, Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.CreditLimitAmount.Should().Be(200m);
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void RaiseCreditLimit_WhenOrderHasNoComanda_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.RaiseCreditLimit(200m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Comanda.LimitTableOrder");
        }

        [Fact]
        public void RaiseCreditLimit_WithAmountNotGreaterThanCurrent_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenComandaOrder(creditLimitAmount: 100m);

            // Act
            var result = order.RaiseCreditLimit(100m, Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("Comanda.LimitMustIncrease");
        }

        // ---------- RemoveServiceFee ----------

        [Fact]
        public void RemoveServiceFee_WithValidArguments_ShouldClearFeeAndRecalculateTotal()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 10m, null, 10, Now); // Subtotal = 100
            order.Close(0.10m, Now); // ServiceFeeAmount = 10, Total = 110

            // Act
            var result = order.RemoveServiceFee(Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.ServiceFeeAmount.Should().Be(0m);
            order.TotalAmount.Should().Be(100.0m);
        }

        [Fact]
        public void RemoveServiceFee_WhenNotAwaitingPayment_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.RemoveServiceFee(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
        }

        [Fact]
        public void RemoveServiceFee_WhenNoServiceFeeApplied_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            order.Close(0m, Now); // ServiceFeeAmount stays 0

            // Act
            var result = order.RemoveServiceFee(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NoServiceFee");
        }

        // ---------- MarkAsPaid / ReopenForPayment / ReopenForConsumption ----------

        [Fact]
        public void MarkAsPaid_WhenAwaitingPayment_ShouldSetPaidAndClosedAt()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            order.Close(0m, Now);

            // Act
            var result = order.MarkAsPaid(Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.OrderStatusId.Should().Be(OrderStatusIds.Pago);
            order.ClosedAt.Should().Be(Now);
        }

        [Fact]
        public void MarkAsPaid_WhenNotAwaitingPayment_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.MarkAsPaid(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
            result.Error.Message.Should().Be("Order is not awaiting payment.");
        }

        [Fact]
        public void ReopenForPayment_WhenPaid_ShouldReturnToAwaitingPayment()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            order.Close(0m, Now);
            order.MarkAsPaid(Now);

            // Act
            var result = order.ReopenForPayment(Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
            order.ClosedAt.Should().BeNull();
        }

        [Fact]
        public void ReopenForPayment_WhenNotPaid_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.ReopenForPayment(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotPaid");
        }

        [Fact]
        public void ReopenForConsumption_WhenAwaitingPayment_ShouldReturnToInProgressAndClearServiceFee()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 10m, null, 10, Now); // Subtotal = 100
            order.Close(0.10m, Now); // ServiceFeeAmount = 10

            // Act
            var result = order.ReopenForConsumption(Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
            order.ServiceFeeAmount.Should().Be(0m);
            order.TotalAmount.Should().Be(100.0m);
        }

        [Fact]
        public void ReopenForConsumption_WhenNotAwaitingPayment_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.ReopenForConsumption(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
        }

        // ---------- Cancel ----------

        [Fact]
        public void Cancel_WhenOpen_ShouldSetCancelledStatusAndClosedAt()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            var result = order.Cancel(Now);

            // Assert
            result.IsSuccess.Should().BeTrue();
            order.OrderStatusId.Should().Be(OrderStatusIds.Cancelado);
            order.ClosedAt.Should().Be(Now);
        }

        [Fact]
        public void Cancel_WhenAlreadyPaid_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.AddItem(100, 10.0m, 1m, null, 10, Now);
            order.Close(0m, Now);
            order.MarkAsPaid(Now);

            // Act
            var result = order.Cancel(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.AlreadyPaid");
        }

        [Fact]
        public void Cancel_WhenAlreadyCancelled_ShouldReturnFailureResult()
        {
            // Arrange
            var order = CreateOpenMesaOrder();
            order.Cancel(Now);

            // Act
            var result = order.Cancel(Now);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Error.Code.Should().Be("CustomerOrder.AlreadyCancelled");
        }

        // ---------- Deactivate ----------

        [Fact]
        public void Deactivate_ShouldUpdateIsActiveToFalseAndSetUpdatedAt()
        {
            // Arrange
            var order = CreateOpenMesaOrder();

            // Act
            order.Deactivate(Now);

            // Assert
            order.IsActive.Should().BeFalse();
            order.UpdatedAt.Should().Be(Now);
        }

        [Fact]
        public void PrivateConstructor_ShouldBeCoveredViaReflection_ForORMSerialization()
        {
            // Arrange & Act
            var instance = Activator.CreateInstance(typeof(CustomerOrder), true) as CustomerOrder;

            // Assert
            instance.Should().NotBeNull();
            instance!.IsActive.Should().BeFalse();
        }
    }
}
