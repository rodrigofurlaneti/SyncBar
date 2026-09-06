using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodIntegrationSettingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodIntegrationSettingRepository _repository;

        public IfoodIntegrationSettingRepositoryTests()
        {
            _repository = new IfoodIntegrationSettingRepository(Context);
        }

        private static IfoodIntegrationSetting CreateSetting(long companyId = 1) =>
            IfoodIntegrationSetting.Create(companyId).Value;

        private async Task<IfoodIntegrationSetting> SeedAsync(IfoodIntegrationSetting setting)
        {
            await Context.AddAsync(setting);
            await Context.SaveChangesAsync();
            Context.Entry(setting).State = EntityState.Detached;
            return setting;
        }

        [Fact]
        public async Task GetByCompanyAsync_ExistingActiveSetting_ReturnsUntrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByCompanyAsync_DifferentTenant_ReturnsNull()
        {
            await SeedAsync(CreateSetting(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoSettingForCompany_ReturnsNull()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyForUpdateAsync_ExistingActiveSetting_ReturnsTrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5));

            var result = await _repository.GetByCompanyForUpdateAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByCompanyForUpdateAsync_NoSetting_ReturnsNull()
        {
            var result = await _repository.GetByCompanyForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetEnabledCompanyIdsAsync_EnabledSetting_ReturnsCompanyId()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5));
            var tracked = await _repository.GetByCompanyForUpdateAsync(5);
            tracked!.SaveCredentials("client-1", "secret-1", enabled: true, ifoodCustomerId: null);
            await Context.SaveChangesAsync();

            var result = await _repository.GetEnabledCompanyIdsAsync();

            result.Should().Contain(5);
        }

        [Fact]
        public async Task GetEnabledCompanyIdsAsync_DisabledSetting_IsExcluded()
        {
            await SeedAsync(CreateSetting(companyId: 5));

            var result = await _repository.GetEnabledCompanyIdsAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetEnabledCompanyIdsAsync_IgnoresTenantFilter_ReturnsCompanyRegardlessOfCurrentTenant()
        {
            var setting = await SeedAsync(CreateSetting(companyId: 5));
            var tracked = await _repository.GetByCompanyForUpdateAsync(5);
            tracked!.SaveCredentials("client-1", "secret-1", enabled: true, ifoodCustomerId: null);
            await Context.SaveChangesAsync();
            TenantService.CompanyId = 6;

            var result = await _repository.GetEnabledCompanyIdsAsync();

            result.Should().Contain(5);
        }

        [Fact]
        public async Task AddAsync_ValidSetting_PersistsToDatabase()
        {
            var setting = CreateSetting(companyId: 7);

            await _repository.AddAsync(setting);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodIntegrationSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
