using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CategoryRepositoryTests : RepositoryTestBase
    {
        private readonly CategoryRepository _repository;

        public CategoryRepositoryTests()
        {
            _repository = new CategoryRepository(Context);
        }

        private static Category CreateCategory(long companyId = 1, string name = "Bebidas", int displayOrder = 1) =>
            Category.Create(companyId, name, displayOrder).Value;

        private async Task<Category> SeedAsync(Category category)
        {
            await Context.AddAsync(category);
            await Context.SaveChangesAsync();
            Context.Entry(category).State = EntityState.Detached;
            return category;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedCategory()
        {
            var category = await SeedAsync(CreateCategory());

            var result = await _repository.GetByIdAsync(category.Id);

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
            var category = await SeedAsync(CreateCategory(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(category.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedCategory()
        {
            var category = await SeedAsync(CreateCategory());

            var result = await _repository.GetByIdForUpdateAsync(category.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveCategories_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateCategory(companyId: 5, name: "Bebidas"));
            var second = await SeedAsync(CreateCategory(companyId: 5, name: "Pratos"));
            await SeedAsync(CreateCategory(companyId: 6, name: "Outra"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveCategory_IsExcluded()
        {
            var category = await SeedAsync(CreateCategory(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(category.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoCategoriesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByCompanyAsync_IncludesInactiveCategories()
        {
            var active = await SeedAsync(CreateCategory(companyId: 5, name: "Ativa"));
            var inactive = await SeedAsync(CreateCategory(companyId: 5, name: "Inativa"));
            var tracked = await _repository.GetByIdForUpdateAsync(inactive.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetAllByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([active.Id, inactive.Id]);
        }

        [Fact]
        public async Task GetAllByCompanyAsync_NoCategoriesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidCategory_PersistsToDatabase()
        {
            var category = CreateCategory(companyId: 7, name: "Nova Categoria");

            await _repository.AddAsync(category);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Category>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Nova Categoria");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
