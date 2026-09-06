using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class KeetaIntegrationSettingRepositoryTests : RepositoryTestBase
    {
        private readonly KeetaIntegrationSettingRepository _repository;

        public KeetaIntegrationSettingRepositoryTests()
        {
            _repository = new KeetaIntegrationSettingRepository(Context);
        }

        private static KeetaIntegrationSetting CreateSetting(long companyId = 1, long branchId = 0) =>
            KeetaIntegrationSetting.Create(companyId, branchId).Value;

        private async Task<KeetaIntegrationSetting> SeedAsync(KeetaIntegrationSetting setting)
        {
            await Context.AddAsync(setting);
            await Context.SaveChangesAsync();
            Context.Entry(setting).State = EntityState.Detached;
            return setting;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting());

            var result = await _repository.GetByIdAsync(setting.Id);

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
        public async Task GetByCompanyIdAsync_CompanyLevelSettingExists_ReturnsSetting()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByCompanyIdAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
        }

        [Fact]
        public async Task GetByCompanyIdAsync_OnlyBranchLevelSettingExists_ReturnsNull()
        {
            await SeedAsync(CreateSetting(companyId: 5, branchId: 10));

            var result = await _repository.GetByCompanyIdAsync(5);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchIdAsync_ExistingBranchSetting_ReturnsSetting()
        {
            var setting = await SeedAsync(CreateSetting(branchId: 10));

            var result = await _repository.GetByBranchIdAsync(10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
        }

        [Fact]
        public async Task GetByBranchIdAsync_NoSettingForBranch_ReturnsNull()
        {
            var result = await _repository.GetByBranchIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_BranchSettingExists_ReturnsBranchSetting()
        {
            var branchSetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 10));
            await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(branchSetting.Id);
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_NoBranchSetting_FallsBackToCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(companySetting.Id);
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_NoBranchIdProvided_ReturnsCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, null);

            result.Should().NotBeNull();
            result!.Id.Should().Be(companySetting.Id);
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_NothingConfigured_ReturnsNull()
        {
            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, 10);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByScopeAsync_BranchIdProvidedAndExists_ReturnsBranchSetting()
        {
            var branchSetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 10));

            var result = await _repository.GetByScopeAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(branchSetting.Id);
        }

        [Fact]
        public async Task GetByScopeAsync_BranchIdProvidedButNoBranchSetting_DoesNotFallBackToCompany()
        {
            await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByScopeAsync(5, 10);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByScopeAsync_NoBranchIdProvided_ReturnsCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByScopeAsync(5, null);

            result.Should().NotBeNull();
            result!.Id.Should().Be(companySetting.Id);
        }

        [Fact]
        public async Task ExistsForCompanyAsync_CompanyLevelSettingExists_ReturnsTrue()
        {
            await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.ExistsForCompanyAsync(5);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsForCompanyAsync_NoSetting_ReturnsFalse()
        {
            var result = await _repository.ExistsForCompanyAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsForBranchAsync_BranchSettingExists_ReturnsTrue()
        {
            await SeedAsync(CreateSetting(branchId: 10));

            var result = await _repository.ExistsForBranchAsync(10);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsForBranchAsync_NoSetting_ReturnsFalse()
        {
            var result = await _repository.ExistsForBranchAsync(999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting());

            var result = await _repository.GetByIdForUpdateAsync(setting.Id);

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
        public async Task GetByCompanyIdForUpdateAsync_ExistingCompanySetting_ReturnsTrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5, branchId: 0));

            var result = await _repository.GetByCompanyIdForUpdateAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByCompanyIdForUpdateAsync_NoSetting_ReturnsNull()
        {
            var result = await _repository.GetByCompanyIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchIdForUpdateAsync_ExistingBranchSetting_ReturnsTrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(branchId: 10));

            var result = await _repository.GetByBranchIdForUpdateAsync(10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchIdForUpdateAsync_NoSetting_ReturnsNull()
        {
            var result = await _repository.GetByBranchIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidSetting_PersistsToDatabase()
        {
            var setting = CreateSetting(companyId: 7);

            await _repository.AddAsync(setting);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingSetting_PersistsChanges()
        {
            var setting = await SeedAsync(CreateSetting());
            var tracked = await _repository.GetByIdForUpdateAsync(setting.Id);
            tracked!.SaveCredentials("client-1", "secret-1", "app-1");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == setting.Id);

            persisted.ClientId.Should().Be("client-1");
        }

        [Fact]
        public async Task Delete_ExistingSetting_RemovesFromDatabase()
        {
            var setting = await SeedAsync(CreateSetting());
            var tracked = await _repository.GetByIdForUpdateAsync(setting.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<KeetaIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == setting.Id);

            persisted.Should().BeNull();
        }
    }
}
