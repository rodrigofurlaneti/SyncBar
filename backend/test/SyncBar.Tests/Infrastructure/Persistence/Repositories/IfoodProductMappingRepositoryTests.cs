using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodProductMappingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodProductMappingRepository _repository;

        public IfoodProductMappingRepositoryTests()
        {
            _repository = new IfoodProductMappingRepository(Context);
        }

        private static IfoodProductMapping CreateMapping(long productId = 1, long branchId = 1) =>
            IfoodProductMapping.Create(productId, branchId).Value;

        private async Task<IfoodProductMapping> SeedAsync(IfoodProductMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByProductAndBranchAsync_ExistingActiveMapping_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(productId: 5, branchId: 10));

            var result = await _repository.GetByProductAndBranchAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByProductAndBranchAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByProductAndBranchAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveMappings_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateMapping(productId: 1, branchId: 5));
            var second = await SeedAsync(CreateMapping(productId: 2, branchId: 5));
            await SeedAsync(CreateMapping(productId: 3, branchId: 6));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_NoMappingsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(productId: 7, branchId: 7);

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodProductMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProductId == 7 && x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
