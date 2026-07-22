using FluentAssertions;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using Xunit;

namespace SyncBar.Tests.Domain;

public sealed class CustomerOrderTests
{
    [Fact]
    public void Create_WithoutTableAndComanda_ShouldFail()
    {
        var result = CustomerOrder.Create(1, null, null, 1, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.MissingOrigin");
    }

    [Fact]
    public void Create_WithTable_ShouldOpenWithStatusAberto()
    {
        var result = CustomerOrder.Create(1, 10, null, 1, 4, null);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderStatusId.Should().Be(OrderStatusIds.Aberto);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AddItem_ShouldFreezeUnitPriceAndRecalculateTotals()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;

        var result = order.AddItem(productId: 5, unitPrice: 14.90m, quantity: 2, notes: null, employeeId: null);

        result.IsSuccess.Should().BeTrue();
        order.Items.Should().HaveCount(1);
        order.Items.First().UnitPrice.Should().Be(14.90m);
        order.SubtotalAmount.Should().Be(29.80m);
        order.TotalAmount.Should().Be(29.80m);
        order.OrderStatusId.Should().Be(OrderStatusIds.EmAndamento);
    }

    [Fact]
    public void AddItem_WithZeroQuantity_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;

        var result = order.AddItem(5, 10m, 0, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.InvalidQuantity");
    }

    [Fact]
    public void ApplyDiscount_GreaterThanSubtotal_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 10m, 1, null, null);

        var result = order.ApplyDiscount(50m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.DiscountExceedsSubtotal");
    }

    [Fact]
    public void Close_ShouldApplyServiceFeeAndSetAwaitingPayment()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 100m, 1, null, null);

        var result = order.Close(serviceFeeRate: 0.10m);

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(10m);
        order.TotalAmount.Should().Be(110m);
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento);
    }

    [Fact]
    public void Close_WithoutItems_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;

        var result = order.Close(0.10m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NoItems");
    }

    [Fact]
    public void AddItem_BeyondComandaLimit_ShouldBeBlocked()
    {
        // Comanda com limite de 150: 100 ja consumidos, item de 60 estoura → bloqueia.
        var order = CustomerOrder.Create(1, null, 37, 1, null, null, creditLimitAmount: 150m).Value;
        order.AddItem(1, 100m, 1, null, null);

        var result = order.AddItem(2, 60m, 1, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitExceeded");
        order.TotalAmount.Should().Be(100m); // nada foi lancado
    }

    [Fact]
    public void AddItem_AfterManagerRaisesLimit_ShouldSucceed()
    {
        var order = CustomerOrder.Create(1, null, 37, 1, null, null, creditLimitAmount: 150m).Value;
        order.AddItem(1, 100m, 1, null, null);

        order.RaiseCreditLimit(250m).IsSuccess.Should().BeTrue();
        var result = order.AddItem(2, 60m, 1, null, null);

        result.IsSuccess.Should().BeTrue();
        order.TotalAmount.Should().Be(160m);
    }

    [Fact]
    public void RaiseCreditLimit_OnTableOrder_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;

        var result = order.RaiseCreditLimit(500m);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Comanda.LimitTableOrder");
    }

    [Fact]
    public void TableOrder_ShouldHaveNoLimit()
    {
        // Mesa nao tem limite mesmo que um valor seja passado.
        var order = CustomerOrder.Create(1, 10, null, 1, null, null, creditLimitAmount: 150m).Value;
        order.CreditLimitAmount.Should().BeNull();

        order.AddItem(1, 999m, 1, null, null).IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void RemoveServiceFee_AfterClose_ShouldZeroFeeAndRecalculate()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 100m, 1, null, null);
        order.Close(0.10m); // total 110

        var result = order.RemoveServiceFee();

        result.IsSuccess.Should().BeTrue();
        order.ServiceFeeAmount.Should().Be(0m);
        order.TotalAmount.Should().Be(100m);
        order.OrderStatusId.Should().Be(OrderStatusIds.AguardandoPagamento); // segue aguardando
    }

    [Fact]
    public void RemoveServiceFee_BeforeClose_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 100m, 1, null, null);

        var result = order.RemoveServiceFee();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NotAwaitingPayment");
    }

    [Fact]
    public void RemoveServiceFee_Twice_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 100m, 1, null, null);
        order.Close(0.10m);
        order.RemoveServiceFee();

        var result = order.RemoveServiceFee();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.NoServiceFee");
    }

    [Fact]
    public void Cancel_PaidOrder_ShouldFail()
    {
        var order = CustomerOrder.Create(1, 10, null, 1, null, null).Value;
        order.AddItem(5, 100m, 1, null, null);
        order.Close(0.10m);
        order.MarkAsPaid();

        var result = order.Cancel();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CustomerOrder.AlreadyPaid");
    }
}
