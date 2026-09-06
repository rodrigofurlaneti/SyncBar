using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodMerchantMappingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodMerchantMappingRepository _repository;

        public IfoodMerchantMappingRepositoryTests()
        {
            _repository = new IfoodMerchantMappingRepository(Context);
        }

        private static IfoodMerchantMapping CreateMapping(long branchId = 1) =>
            IfoodMerchantMapping.Create(branchId).Value;

        private async Task<IfoodMerchantMapping> SeedAsync(IfoodMerchantMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByBranchAsync_ExistingActiveMapping_ReturnsUntrackedMapping()
        {
            var mapping = await SeedAsync(CreateMapping(branchId: 5));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByBranchAsync_NoMappingForBranch_ReturnsNull()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_ExistingActiveMapping_ReturnsTrackedMapping()
        {
            var mapping = await SeedAsync(CreateMapping(branchId: 5));

            var result = await _repository.GetByBranchForUpdateAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_NoMapping_ReturnsNull()
        {
            var result = await _repository.GetByBranchForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_MultipleBranchesWithMappings_ReturnsDictionaryKeyedByBranch()
        {
            var branchOne = Branch.Create(5, "Filial 1", null, null, null, null, null, null, null, null).Value;
            var branchTwo = Branch.Create(5, "Filial 2", null, null, null, null, null, null, null, null).Value;
            await Context.AddRangeAsync(branchOne, branchTwo);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateMapping(branchId: branchOne.Id));
            await SeedAsync(CreateMapping(branchId: branchTwo.Id));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Should().ContainKey(branchOne.Id);
            result.Should().ContainKey(branchTwo.Id);
        }

        [Fact]
        public async Task GetByCompanyAsync_BranchInactive_IsExcluded()
        {
            var branch = Branch.Create(5, "Filial 1", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateMapping(branchId: branch.Id));
            var trackedBranch = await Context.Branchs.FirstAsync(x => x.Id == branch.Id);
            trackedBranch.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoBranchesForCompany_ReturnsEmptyDictionary()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(branchId: 7);

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
