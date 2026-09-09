using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Application.Features.Orders;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories;

public sealed class OrderItemCustomizationTests : RepositoryTestBase
{
    [Fact]
    public async Task ChoicesSurviveReloadCatalogEditsAndTransferWithoutDoubleCharging()
    {
        var now = DateTime.Now;
        var product = Product.Create(1, 1, 1, "Coca", null, null, 20m, null, false, null).Value;
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        product.ToggleExtrasAndBoosts(true, true);
        product.OptionalExtras.Add(ProductOptionalExtra.Create(product.Id, "Gelo", 0).Value);
        product.Boosts.Add(ProductBoost.Create(product.Id, "Laranja", 2m, 0).Value);
        await Context.SaveChangesAsync();
        var order = CustomerOrder.Create(1, 1, null, 1, null, null, now).Value;
        order.AddItemWithPromotion(product, 2, null, null, 1, now, product.OptionalExtras.ToArray(), product.Boosts.ToArray()).IsSuccess.Should().BeTrue();
        Context.CustomerOrders.Add(order);
        await Context.SaveChangesAsync();
        var orderId = order.Id;
        product.OptionalExtras.Single().Update("Outro opcional", 0);
        product.Boosts.Single().Update("Outro adicional", 10m, 0);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();

        var repository = new CustomerOrderRepository(Context);
        var loaded = (await repository.GetByIdAsync(orderId))!;
        var item = loaded.Items.Single();
        var response = loaded.ToResponse();
        response.Items.Single().OptionalExtras.Single().Name.Should().Be("Gelo");
        response.Items.Single().Boosts.Single().UnitPriceCharged.Should().Be(2m);
        response.TotalAmount.Should().Be(44m);
        item.GetPreparationNotes().Should().Contain("Gelo").And.Contain("Laranja");

        var target = CustomerOrder.Create(1, 2, null, 1, null, null, now).Value;
        target.AddTransferredItem(item.ProductId, item.UnitPrice, item.Quantity, item.Notes,
            null, item.OrderItemStatusId, now, item).IsSuccess.Should().BeTrue();
        Context.CustomerOrders.Add(target);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var transferred = (await repository.GetByIdAsync(target.Id))!;
        transferred.Items.Single().Boosts.Single().Name.Should().Be("Laranja");
        transferred.Items.Single().OptionalExtras.Single().Name.Should().Be("Gelo");
        transferred.TotalAmount.Should().Be(44m);
    }
}
