using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ProductComplementGroupRepositoryTests : RepositoryTestBase
    {
        private readonly ProductComplementGroupRepository _repository;

        public ProductComplementGroupRepositoryTests()
        {
            _repository = new ProductComplementGroupRepository(Context);
        }

        private static ProductComplementGroup CreateLink(long productId = 1, long complementGroupId = 1, int displayOrder = 0) =>
            ProductComplementGroup.Create(productId, complementGroupId, displayOrder).Value;

        private async Task<ProductComplementGroup> SeedAsync(ProductComplementGroup link)
        {
            await Context.AddAsync(link);
            await Context.SaveChangesAsync();
            Context.Entry(link).State = EntityState.Detached;
            return link;
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedLink()
        {
            var link = await SeedAsync(CreateLink());

            var result = await _repository.GetByIdForUpdateAsync(link.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByProductForUpdateAsync_IncludesInactiveLinks()
        {
            var link = await SeedAsync(CreateLink(productId: 5));
            var tracked = (await _repository.GetByProductForUpdateAsync(5)).Single();
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ProductComplementGroup>().AsNoTracking().FirstAsync(x => x.Id == link.Id);

            persisted.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByProductForUpdateAsync_NoLinks_ReturnsEmptyList()
        {
            var result = await _repository.GetByProductForUpdateAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByProductAsync_MultipleActiveLinks_ReturnsOrderedByDisplayOrder()
        {
            var second = await SeedAsync(CreateLink(productId: 5, complementGroupId: 1, displayOrder: 2));
            var first = await SeedAsync(CreateLink(productId: 5, complementGroupId: 2, displayOrder: 1));

            var result = await _repository.GetByProductAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(first.Id);
            result.ElementAt(1).Id.Should().Be(second.Id);
        }

        [Fact]
        public async Task GetByProductAsync_InactiveLink_IsExcluded()
        {
            var link = await SeedAsync(CreateLink(productId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(link.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByProductAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByProductAsync_NoLinksForProduct_ReturnsEmptyList()
        {
            var result = await _repository.GetByProductAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByComplementGroupAsync_MultipleActiveLinks_ReturnsAllForGroup()
        {
            var first = await SeedAsync(CreateLink(productId: 1, complementGroupId: 5));
            var second = await SeedAsync(CreateLink(productId: 2, complementGroupId: 5));
            await SeedAsync(CreateLink(productId: 3, complementGroupId: 6));

            var result = await _repository.GetByComplementGroupAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByComplementGroupAsync_NoLinksForGroup_ReturnsEmptyList()
        {
            var result = await _repository.GetByComplementGroupAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByProductsAsync_MatchingProducts_ReturnsOrderedByDisplayOrder()
        {
            var second = await SeedAsync(CreateLink(productId: 5, displayOrder: 2));
            var first = await SeedAsync(CreateLink(productId: 6, displayOrder: 1));
            await SeedAsync(CreateLink(productId: 7, displayOrder: 0));

            var result = await _repository.GetByProductsAsync([5, 6]);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(first.Id);
            result.ElementAt(1).Id.Should().Be(second.Id);
        }

        [Fact]
        public async Task GetByProductsAsync_NoMatchingProducts_ReturnsEmptyList()
        {
            var result = await _repository.GetByProductsAsync([999]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidLink_PersistsToDatabase()
        {
            var link = CreateLink(productId: 10, complementGroupId: 20);

            await _repository.AddAsync(link);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ProductComplementGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == 10 && x.ComplementGroupId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
