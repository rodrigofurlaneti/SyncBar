using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodCategoryMappingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodCategoryMappingRepository _repository;

        public IfoodCategoryMappingRepositoryTests()
        {
            _repository = new IfoodCategoryMappingRepository(Context);
        }

        private static IfoodCategoryMapping CreateMapping(long categoryId = 1, long branchId = 1, string ifoodCategoryId = "ifc-1") =>
            IfoodCategoryMapping.Create(categoryId, branchId, ifoodCategoryId).Value;

        private async Task<IfoodCategoryMapping> SeedAsync(IfoodCategoryMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByCategoryAndBranchAsync_ExistingActiveMapping_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(categoryId: 5, branchId: 10));

            var result = await _repository.GetByCategoryAndBranchAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByCategoryAndBranchAsync_DifferentBranch_ReturnsNull()
        {
            await SeedAsync(CreateMapping(categoryId: 5, branchId: 10));

            var result = await _repository.GetByCategoryAndBranchAsync(5, 20);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCategoryAndBranchAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByCategoryAndBranchAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(categoryId: 7, branchId: 7, ifoodCategoryId: "ifc-new");

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodCategoryMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodCategoryId == "ifc-new");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
