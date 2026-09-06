using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ComplementItemRepositoryTests : RepositoryTestBase
    {
        private readonly ComplementItemRepository _repository;

        public ComplementItemRepositoryTests()
        {
            _repository = new ComplementItemRepository(Context);
        }

        private static ComplementItem CreateItem(long companyId = 1, string name = "Queijo Extra") =>
            ComplementItem.Create(companyId, name).Value;

        private async Task<ComplementItem> SeedAsync(ComplementItem item)
        {
            await Context.AddAsync(item);
            await Context.SaveChangesAsync();
            Context.Entry(item).State = EntityState.Detached;
            return item;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedItem()
        {
            var item = await SeedAsync(CreateItem());

            var result = await _repository.GetByIdAsync(item.Id);

            result.Should().NotBeNull();
            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_DifferentTenant_ReturnsNull()
        {
            var item = await SeedAsync(CreateItem(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(item.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedItem()
        {
            var item = await SeedAsync(CreateItem());

            var result = await _repository.GetByIdForUpdateAsync(item.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveItems_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateItem(companyId: 5, name: "I1"));
            var second = await SeedAsync(CreateItem(companyId: 5, name: "I2"));
            await SeedAsync(CreateItem(companyId: 6, name: "I3"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveItem_IsExcluded()
        {
            var item = await SeedAsync(CreateItem(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(item.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoItemsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_MatchingIds_ReturnsItems()
        {
            var first = await SeedAsync(CreateItem(name: "I1"));
            var second = await SeedAsync(CreateItem(name: "I2"));
            await SeedAsync(CreateItem(name: "I3"));

            var result = await _repository.GetByIdsAsync([first.Id, second.Id]);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdsAsync_NoMatchingIds_ReturnsEmptyList()
        {
            var result = await _repository.GetByIdsAsync([999]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidItem_PersistsToDatabase()
        {
            var item = CreateItem(companyId: 7, name: "Novo Complemento");

            await _repository.AddAsync(item);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ComplementItem>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Novo Complemento");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
