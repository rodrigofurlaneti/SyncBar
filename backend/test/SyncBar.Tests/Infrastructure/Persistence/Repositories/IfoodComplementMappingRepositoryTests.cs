using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodComplementMappingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodComplementMappingRepository _repository;

        public IfoodComplementMappingRepositoryTests()
        {
            _repository = new IfoodComplementMappingRepository(Context);
        }

        private static IfoodComplementMapping CreateMapping(long complementId = 1, long branchId = 1) =>
            IfoodComplementMapping.Create(complementId, branchId).Value;

        private async Task<IfoodComplementMapping> SeedAsync(IfoodComplementMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByComplementAndBranchAsync_ExistingActiveMapping_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(complementId: 5, branchId: 10));

            var result = await _repository.GetByComplementAndBranchAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByComplementAndBranchAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByComplementAndBranchAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveMappings_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateMapping(complementId: 1, branchId: 5));
            var second = await SeedAsync(CreateMapping(complementId: 2, branchId: 5));
            await SeedAsync(CreateMapping(complementId: 3, branchId: 6));

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
        public async Task GetByIfoodOptionIdAndBranchAsync_ExistingActiveMapping_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(complementId: 1, branchId: 10));

            var result = await _repository.GetByIfoodOptionIdAndBranchAsync(mapping.IfoodOptionId, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByIfoodOptionIdAndBranchAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByIfoodOptionIdAndBranchAsync(Guid.NewGuid(), 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(complementId: 7, branchId: 7);

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodComplementMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ComplementId == 7 && x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
