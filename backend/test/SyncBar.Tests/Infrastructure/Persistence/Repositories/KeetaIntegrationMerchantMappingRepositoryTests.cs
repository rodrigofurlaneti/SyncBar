using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationMerchantMappingRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationMerchantMappingRepository _repository;

        public KeetaIntegrationMerchantMappingRepositoryTests()
        {
            _repository = new KeetaIntegrationMerchantMappingRepository(Context);
        }

        private static KeetaIntegrationMerchantMapping CreateMapping(
            long companyId = 1, long branchId = 1, string internalMerchantId = "im-1",
            long keetaMerchantId = 100, string storeName = "Loja 1") =>
            KeetaIntegrationMerchantMapping.Create(companyId, branchId, internalMerchantId, keetaMerchantId, storeName).Value;

        private async Task<KeetaIntegrationMerchantMapping> SeedAsync(KeetaIntegrationMerchantMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedMapping()
        {
            var mapping = await SeedAsync(CreateMapping());

            var result = await _repository.GetByIdAsync(mapping.Id);

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
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedMapping()
        {
            var mapping = await SeedAsync(CreateMapping());

            var result = await _repository.GetByIdForUpdateAsync(mapping.Id);

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
        public async Task GetByKeetaMerchantIdAsync_ExistingId_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(keetaMerchantId: 555));

            var result = await _repository.GetByKeetaMerchantIdAsync(555);

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByKeetaMerchantIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByKeetaMerchantIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByInternalMerchantIdAsync_ExistingId_ReturnsMapping()
        {
            var mapping = await SeedAsync(CreateMapping(internalMerchantId: "im-abc"));

            var result = await _repository.GetByInternalMerchantIdAsync("im-abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(mapping.Id);
        }

        [Fact]
        public async Task GetByInternalMerchantIdAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByInternalMerchantIdAsync("im-missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_MultipleMappings_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateMapping(companyId: 5, keetaMerchantId: 1));
            var second = await SeedAsync(CreateMapping(companyId: 5, keetaMerchantId: 2));
            await SeedAsync(CreateMapping(companyId: 6, keetaMerchantId: 3));

            var result = await _repository.GetAllByCompanyIdAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_NoMappingsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByCompanyIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByBranchIdAsync_MultipleMappings_ReturnsAllForBranch()
        {
            var mapping = await SeedAsync(CreateMapping(branchId: 10, keetaMerchantId: 1));
            await SeedAsync(CreateMapping(branchId: 20, keetaMerchantId: 2));

            var result = await _repository.GetAllByBranchIdAsync(10);

            result.Should().ContainSingle(x => x.Id == mapping.Id);
        }

        [Fact]
        public async Task GetAllByBranchIdAsync_NoMappingsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByKeetaMerchantIdAsync_Existing_ReturnsTrue()
        {
            await SeedAsync(CreateMapping(keetaMerchantId: 777));

            var result = await _repository.ExistsByKeetaMerchantIdAsync(777);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByKeetaMerchantIdAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByKeetaMerchantIdAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(keetaMerchantId: 888);

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.KeetaMerchantId == 888);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingMapping_PersistsChanges()
        {
            var mapping = await SeedAsync(CreateMapping());
            var tracked = await _repository.GetByIdForUpdateAsync(mapping.Id);
            tracked!.UpdateStatus(isAuthorized: false, isOnboarded: true);

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == mapping.Id);

            persisted.IsAuthorized.Should().BeFalse();
            persisted.IsOnboarded.Should().BeTrue();
        }

        [Fact]
        public async Task Delete_ExistingMapping_RemovesFromDatabase()
        {
            var mapping = await SeedAsync(CreateMapping());
            var tracked = await _repository.GetByIdForUpdateAsync(mapping.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationMerchantMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == mapping.Id);

            persisted.Should().BeNull();
        }
    }
}
