using FluentAssertions;
using SyncBar.Application.Features.Integrations.Ifood.Orders;
using Xunit;

namespace SyncBar.Tests.Application.Features.Integrations.Ifood.Orders;

public sealed class GetIFoodOrderVirtualBagQueryTests
{
    [Fact]
    public void GetIfoodOrderVirtualBagQuery_Constructor_ShouldAssignIfoodOrderId()
    {
        var query = new GetIfoodOrderVirtualBagQuery(42);

        query.IfoodOrderId.Should().Be(42);
    }

    [Fact]
    public void IfoodVirtualBagItemResponse_Constructor_ShouldAssignAllProperties()
    {
        var item = new IfoodVirtualBagItemResponse("u-1", "Cerveja", 3, "7890000000001");

        item.UniqueId.Should().Be("u-1");
        item.Name.Should().Be("Cerveja");
        item.Quantity.Should().Be(3);
        item.Ean.Should().Be("7890000000001");
    }

    [Fact]
    public void IfoodOrderVirtualBagResponse_Constructor_ShouldAssignAllProperties()
    {
        var createdAt = DateTime.Now;
        var items = new List<IfoodVirtualBagItemResponse> { new("u-1", "Cerveja", 1, null) };

        var response = new IfoodOrderVirtualBagResponse(
            "bag-1", "ABC123", "PLACED", createdAt, "Bar do Zé", "Maria Silva",
            items, "45.00", "BRL", "{}");

        response.Id.Should().Be("bag-1");
        response.ShortCode.Should().Be("ABC123");
        response.Status.Should().Be("PLACED");
        response.CreatedAt.Should().Be(createdAt);
        response.MerchantName.Should().Be("Bar do Zé");
        response.CustomerName.Should().Be("Maria Silva");
        response.Items.Should().BeSameAs(items);
        response.GrossValueAmount.Should().Be("45.00");
        response.GrossValueCurrency.Should().Be("BRL");
        response.RawPayload.Should().Be("{}");
    }
}
