using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ComandaSettingRepositoryTests : RepositoryTestBase
    {
        private readonly ComandaSettingRepository _repository;

        public ComandaSettingRepositoryTests()
        {
            _repository = new ComandaSettingRepository(Context);
        }

        private static ComandaSetting CreateSetting(long branchId = 1, decimal defaultLimitAmount = 100m) =>
            ComandaSetting.Create(branchId, defaultLimitAmount).Value;

        private async Task<ComandaSetting> SeedAsync(ComandaSetting setting)
        {
            await Context.AddAsync(setting);
            await Context.SaveChangesAsync();
            Context.Entry(setting).State = EntityState.Detached;
            return setting;
        }

        [Fact]
        public async Task GetByBranchAsync_ExistingActiveSetting_ReturnsUntrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(branchId: 5));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByBranchAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateSetting(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByBranchAsync(branch.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_NoSettingForBranch_ReturnsNull()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_ExistingActiveSetting_ReturnsTrackedSetting()
        {
            var setting = await SeedAsync(CreateSetting(branchId: 5));

            var result = await _repository.GetByBranchForUpdateAsync(5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(setting.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByBranchForUpdateAsync_NoSettingForBranch_ReturnsNull()
        {
            var result = await _repository.GetByBranchForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidSetting_PersistsToDatabase()
        {
            var setting = CreateSetting(branchId: 7, defaultLimitAmount: 250m);

            await _repository.AddAsync(setting);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ComandaSetting>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.BranchId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
