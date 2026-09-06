using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AsaasIntegrationSettingRepositoryTests : RepositoryTestBase
    {
        private readonly AsaasIntegrationSettingRepository _repository;

        public AsaasIntegrationSettingRepositoryTests()
        {
            _repository = new AsaasIntegrationSettingRepository(Context);
        }

        private static AsaasIntegrationSetting CreateSetting(
            long companyId = 1,
            long? branchId = null,
            bool isActive = true,
            string apiKey = "key-1") =>
            AsaasIntegrationSetting.Create(companyId, branchId, apiKey, "webhook-1", "Sandbox", "wallet-1", isActive).Value;

        private async Task<AsaasIntegrationSetting> SeedAsync(AsaasIntegrationSetting setting)
        {
            await Context.AddAsync(setting);
            await Context.SaveChangesAsync();
            Context.Entry(setting).State = EntityState.Detached;
            return setting;
        }

        // ---------- GetByIdAsync ----------

        [Fact]
        public async Task GetByIdAsync_ExistingActiveId_ReturnsSetting()
        {
            var setting = await SeedAsync(CreateSetting());

            var result = await _repository.GetByIdAsync(setting.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedEntity()
        {
            var setting = await SeedAsync(CreateSetting());

            var result = await _repository.GetByIdAsync(setting.Id);

            Context.Entry(result!).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_InactiveId_ReturnsNull()
        {
            var setting = await SeedAsync(CreateSetting(isActive: false));

            var result = await _repository.GetByIdAsync(setting.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        // ---------- GetByCompanyIdAsync ----------

        [Fact]
        public async Task GetByCompanyIdAsync_CompanyLevelSettingExists_ReturnsSetting()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5, branchId: null));

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
        public async Task GetByCompanyIdAsync_NoSettingForCompany_ReturnsNull()
        {
            var result = await _repository.GetByCompanyIdAsync(999);

            result.Should().BeNull();
        }

        // ---------- GetByBranchIdAsync ----------

        [Fact]
        public async Task GetByBranchIdAsync_ExistingActiveBranchSetting_ReturnsSetting()
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

        // ---------- GetByBranchOrCompanyFallbackAsync ----------

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_BranchSettingExists_ReturnsBranchSetting()
        {
            var branchSetting = await SeedAsync(CreateSetting(companyId: 5, branchId: 10, apiKey: "branch-key"));
            await SeedAsync(CreateSetting(companyId: 5, branchId: null, apiKey: "company-key"));

            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(branchSetting.Id);
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_NoBranchSetting_FallsBackToCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: null));

            var result = await _repository.GetByBranchOrCompanyFallbackAsync(5, 10);

            result.Should().NotBeNull();
            result!.Id.Should().Be(companySetting.Id);
        }

        [Fact]
        public async Task GetByBranchOrCompanyFallbackAsync_NoBranchIdProvided_ReturnsCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: null));

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

        // ---------- GetByScopeAsync ----------

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
            await SeedAsync(CreateSetting(companyId: 5, branchId: null));

            var result = await _repository.GetByScopeAsync(5, 10);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByScopeAsync_NoBranchIdProvided_ReturnsCompanySetting()
        {
            var companySetting = await SeedAsync(CreateSetting(companyId: 5, branchId: null));

            var result = await _repository.GetByScopeAsync(5, null);

            result.Should().NotBeNull();
            result!.Id.Should().Be(companySetting.Id);
        }

        // ---------- GetAllActiveAsync ----------

        [Fact]
        public async Task GetAllActiveAsync_MixOfActiveAndInactive_ReturnsOnlyActive()
        {
            var active = await SeedAsync(CreateSetting(companyId: 1));
            await SeedAsync(CreateSetting(companyId: 2, isActive: false));

            var result = await _repository.GetAllActiveAsync();

            result.Should().ContainSingle(x => x.Id == active.Id);
        }

        [Fact]
        public async Task GetAllActiveAsync_NoSettings_ReturnsEmptyList()
        {
            var result = await _repository.GetAllActiveAsync();

            result.Should().BeEmpty();
        }

        // ---------- GetAllActiveByCompanyIdAsync ----------

        [Fact]
        public async Task GetAllActiveByCompanyIdAsync_MultipleSettingsSameCompany_ReturnsAllActiveForCompany()
        {
            var branchOne = await SeedAsync(CreateSetting(companyId: 5, branchId: 10));
            var branchTwo = await SeedAsync(CreateSetting(companyId: 5, branchId: 20));
            await SeedAsync(CreateSetting(companyId: 6, branchId: 30));

            var result = await _repository.GetAllActiveByCompanyIdAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([branchOne.Id, branchTwo.Id]);
        }

        [Fact]
        public async Task GetAllActiveByCompanyIdAsync_NoSettingsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllActiveByCompanyIdAsync(999);

            result.Should().BeEmpty();
        }

        // ---------- ExistsForCompanyAsync ----------

        [Fact]
        public async Task ExistsForCompanyAsync_ActiveCompanySettingExists_ReturnsTrue()
        {
            await SeedAsync(CreateSetting(companyId: 5, branchId: null));

            var result = await _repository.ExistsForCompanyAsync(5);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsForCompanyAsync_OnlyInactiveSettingExists_ReturnsFalse()
        {
            await SeedAsync(CreateSetting(companyId: 5, branchId: null, isActive: false));

            var result = await _repository.ExistsForCompanyAsync(5);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsForCompanyAsync_NoSetting_ReturnsFalse()
        {
            var result = await _repository.ExistsForCompanyAsync(999);

            result.Should().BeFalse();
        }

        // ---------- ExistsForBranchAsync ----------

        [Fact]
        public async Task ExistsForBranchAsync_ActiveBranchSettingExists_ReturnsTrue()
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

        // ---------- GetByIdForUpdateAsync ----------

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedEntity()
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

        // ---------- GetByCompanyIdForUpdateAsync ----------

        [Fact]
        public async Task GetByCompanyIdForUpdateAsync_ExistingCompanySetting_ReturnsTrackedEntity()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5, branchId: null));

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

        // ---------- GetByBranchIdForUpdateAsync ----------

        [Fact]
        public async Task GetByBranchIdForUpdateAsync_ExistingBranchSetting_ReturnsTrackedEntity()
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

        // ---------- AddAsync ----------

        [Fact]
        public async Task AddAsync_ValidEntity_PersistsToDatabase()
        {
            var setting = CreateSetting(companyId: 7);

            await _repository.AddAsync(setting);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        // ---------- Update ----------

        [Fact]
        public async Task Update_ExistingEntity_PersistsChanges()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(setting.Id);
            tracked!.UpdateDetails(apiKeyEncrypted: "new-key");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == setting.Id);

            persisted.ApiKeyEncrypted.Should().Be("new-key");
        }

        // ---------- Delete ----------

        [Fact]
        public async Task Delete_ExistingEntity_RemovesFromDatabase()
        {
            var setting = await SeedAsync(CreateSetting());
            var tracked = await _repository.GetByIdForUpdateAsync(setting.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == setting.Id);

            persisted.Should().BeNull();
        }
    }
}
