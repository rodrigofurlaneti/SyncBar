using FluentAssertions;
using SyncBar.Application.Features.Catalog;
using SyncBar.Application.Features.Storefront.GetBranchMenu;
using Xunit;

namespace SyncBar.Tests.Application.Features.Storefront.GetBranchMenu;

public sealed class BranchMenuResponseTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        var items = new List<MenuItemResponse>
        {
            new(1, 5, "Lanches", 1, "X-Burguer", "Delicioso", null, 20m, 10m, false, 15, null, [])
        };

        var response = new BranchMenuResponse("Filial Centro", items);

        response.BranchName.Should().Be("Filial Centro");
        response.Items.Should().BeSameAs(items);
        response.Items.Should().ContainSingle();
    }
}
