using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories;

public sealed class ProductExtrasPersistenceTests : RepositoryTestBase
{
    [Fact]
    public async Task TrackedAggregate_PersistsChildrenAndSoftDelete_AndEnforcesTenantScope()
    {
        var product = Product.Create(1, 1, 1, "Drink", null, null, 10, null, false, null).Value;
        Context.Products.Add(product);
        await Context.SaveChangesAsync();
        var id = product.Id;
        Context.ChangeTracker.Clear();
        var repository = new ProductRepository(Context);
        var tracked = (await repository.GetByIdForUpdateAsync(id))!;
        tracked.OptionalExtras.Add(ProductOptionalExtra.Create(id, "Ice", 1).Value);
        tracked.Boosts.Add(ProductBoost.Create(id, "Double", 3.50m, 0).Value);
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        var saved = (await repository.GetByIdAsync(id))!;
        saved.OptionalExtras.Should().ContainSingle().Which.Id.Should().BeGreaterThan(0);
        saved.Boosts.Should().ContainSingle().Which.IncrementalValue.Should().Be(3.50m);
        var updated = (await repository.GetByIdForUpdateAsync(id))!;
        updated.OptionalExtras.Single().Deactivate();
        await Context.SaveChangesAsync();
        Context.ChangeTracker.Clear();
        (await repository.GetByIdAsync(id))!.OptionalExtras.Single().IsActive.Should().BeFalse();
        TenantService.CompanyId = 2;
        (await repository.GetByIdAsync(id)).Should().BeNull();
        (await repository.GetByIdForUpdateAsync(id)).Should().BeNull();
        Context.Model.FindEntityType(typeof(ProductBoost))!.GetForeignKeys()
            .Single().DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }
}
