using FluentAssertions;
using Reqnroll;
using SyncBar.Domain.Constants;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Primitives;

namespace SyncBar.Specs.StepDefinitions;

[Binding]
public sealed class CustomerOrderSteps
{
    private Result<CustomerOrder>? _createResult;
    private CustomerOrder? _order;

    [When(@"I open an order without a table and without a comanda")]
    public void WhenIOpenAnOrderWithoutTableAndComanda()
        => _createResult = CustomerOrder.Create(1, null, null, 1, null, null);

    [When(@"I open an order for table (.*)")]
    public void WhenIOpenAnOrderForTable(long tableId)
    {
        _createResult = CustomerOrder.Create(1, tableId, null, 1, null, null);
        _order = _createResult.IsSuccess ? _createResult.Value : null;
    }

    [Given(@"an open order for table (.*)")]
    public void GivenAnOpenOrderForTable(long tableId)
        => _order = CustomerOrder.Create(1, tableId, null, 1, null, null).Value;

    [Given(@"the order has (.*) unit of a product priced at (.*)")]
    public void GivenTheOrderHasUnits(decimal quantity, decimal price)
        => _order!.AddItem(1, price, quantity, null, null);

    [When(@"I add (.*) units of a product priced at (.*)")]
    public void WhenIAddUnits(decimal quantity, decimal price)
        => _order!.AddItem(1, price, quantity, null, null);

    [When(@"I close the order with a service fee of (.*) percent")]
    public void WhenICloseTheOrder(decimal percent)
        => _order!.Close(percent / 100m);

    [Then(@"the order creation should fail with error ""(.*)""")]
    public void ThenTheOrderCreationShouldFail(string errorCode)
    {
        _createResult!.IsFailure.Should().BeTrue();
        _createResult.Error.Code.Should().Be(errorCode);
    }

    [Then(@"the order should be created with status ""(.*)""")]
    public void ThenTheOrderShouldBeCreatedWithStatus(string status)
    {
        _order.Should().NotBeNull();
        var expected = status switch
        {
            "Aberto" => OrderStatusIds.Aberto,
            "EmAndamento" => OrderStatusIds.EmAndamento,
            "AguardandoPagamento" => OrderStatusIds.AguardandoPagamento,
            _ => throw new ArgumentOutOfRangeException(nameof(status))
        };
        _order!.OrderStatusId.Should().Be(expected);
    }

    [Then(@"the order subtotal should be (.*)")]
    public void ThenTheOrderSubtotalShouldBe(decimal subtotal)
        => _order!.SubtotalAmount.Should().Be(subtotal);

    [Then(@"the order total should be (.*)")]
    public void ThenTheOrderTotalShouldBe(decimal total)
        => _order!.TotalAmount.Should().Be(total);

    [Then(@"the order status should be ""(.*)""")]
    public void ThenTheOrderStatusShouldBe(string status)
        => ThenTheOrderShouldBeCreatedWithStatus(status);
}
