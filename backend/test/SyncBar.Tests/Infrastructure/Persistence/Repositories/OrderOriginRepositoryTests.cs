using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class OrderOriginRepositoryTests : RepositoryTestBase
    {
        private readonly OrderOriginRepository _repository;

        public OrderOriginRepositoryTests()
        {
            _repository = new OrderOriginRepository(Context);
        }

        private static OrderOrigin CreateOrigin(long? companyId, long? branchId, string name, DateTime? now = null)
            => OrderOrigin.Create(companyId, branchId, name, now ?? DateTime.Now).Value;

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsEntityUntracked()
        {
            var origin = CreateOrigin(null, null, "LOCAL");
            await Context.AddAsync(origin);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(origin.Id);

            result.Should().NotBeNull();
            result!.Name.Should().Be("LOCAL");
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedEntity()
        {
            var origin = CreateOrigin(null, null, "WEBSITE");
            await Context.AddAsync(origin);
            await Context.SaveChangesAsync();
            Context.Entry(origin).State = EntityState.Detached;

            var result = await _repository.GetByIdForUpdateAsync(origin.Id);

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
        public async Task GetAllAsync_MultipleOrigins_ReturnsAllOrderedByName()
        {
            await Context.AddRangeAsync(
                CreateOrigin(null, null, "WEBSITE"),
                CreateOrigin(null, null, "IFOOD"),
                CreateOrigin(null, null, "LOCAL"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Select(x => x.Name).Should().ContainInOrder("IFOOD", "LOCAL", "WEBSITE");
        }

        [Fact]
        public async Task GetAllAsync_NoOrigins_ReturnsEmptyList()
        {
            var result = await _repository.GetAllAsync();

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAndBranchAsync_GlobalOrigin_IsReturnedForAnyCompanyAndBranch()
        {
            await Context.AddAsync(CreateOrigin(null, null, "LOCAL"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAndBranchAsync(1, 1);

            result.Should().ContainSingle(x => x.Name == "LOCAL");
        }

        [Fact]
        public async Task GetByCompanyAndBranchAsync_OriginScopedToOtherCompany_IsNotReturned()
        {
            await Context.AddAsync(CreateOrigin(2, 2, "CUSTOM"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAndBranchAsync(1, 1);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAndBranchAsync_OriginScopedToRequestedCompany_IsReturned()
        {
            await Context.AddAsync(CreateOrigin(1, 1, "CUSTOM"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAndBranchAsync(1, 1);

            result.Should().ContainSingle(x => x.Name == "CUSTOM");
        }

        [Fact]
        public async Task GetFilteredAsync_NoFilters_ReturnsAllOrderedByName()
        {
            await Context.AddRangeAsync(
                CreateOrigin(null, null, "WEBSITE"),
                CreateOrigin(null, null, "IFOOD"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetFilteredAsync(null, null, null, null);

            result.Select(x => x.Name).Should().ContainInOrder("IFOOD", "WEBSITE");
        }

        [Fact]
        public async Task GetFilteredAsync_WithCompanyFilter_ExcludesOriginsFromOtherCompanies()
        {
            await Context.AddRangeAsync(
                CreateOrigin(null, null, "LOCAL"),
                CreateOrigin(2, null, "OTHER_COMPANY"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetFilteredAsync(1, null, null, null);

            result.Select(x => x.Name).Should().Contain("LOCAL").And.NotContain("OTHER_COMPANY");
        }

        [Fact]
        public async Task GetFilteredAsync_WithBranchFilter_ExcludesOriginsFromOtherBranches()
        {
            await Context.AddRangeAsync(
                CreateOrigin(null, null, "LOCAL"),
                CreateOrigin(null, 2, "OTHER_BRANCH"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetFilteredAsync(null, 1, null, null);

            result.Select(x => x.Name).Should().Contain("LOCAL").And.NotContain("OTHER_BRANCH");
        }

        [Fact]
        public async Task GetFilteredAsync_WithSearchTerm_ReturnsOnlyMatchingNames()
        {
            await Context.AddRangeAsync(
                CreateOrigin(null, null, "IFOOD"),
                CreateOrigin(null, null, "WEBSITE"));
            await Context.SaveChangesAsync();

            var result = await _repository.GetFilteredAsync(null, null, "foo", null);

            result.Should().ContainSingle(x => x.Name == "IFOOD");
        }

        [Fact]
        public async Task GetFilteredAsync_WithIsActiveFilter_ReturnsOnlyMatchingStatus()
        {
            var active = CreateOrigin(null, null, "LOCAL");
            var inactive = CreateOrigin(null, null, "LEGACY");
            // OrderOrigin não expõe um método público para desativar (sem Deactivate()) — força via
            // reflection só para montar o cenário do teste, igual ao padrão já usado noutros testes
            // deste projeto para propriedades sem setter público.
            typeof(OrderOrigin).GetProperty(nameof(OrderOrigin.IsActive))!.SetValue(inactive, false);
            await Context.AddRangeAsync(active, inactive);
            await Context.SaveChangesAsync();

            var result = await _repository.GetFilteredAsync(null, null, null, true);

            result.Select(x => x.Name).Should().Contain("LOCAL").And.NotContain("LEGACY");
        }

        [Fact]
        public async Task ExistsByNameAsync_MatchingNameSameScope_ReturnsTrue()
        {
            await Context.AddAsync(CreateOrigin(1, 1, "LOCAL"));
            await Context.SaveChangesAsync();

            var result = await _repository.ExistsByNameAsync(1, 1, "local");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameAsync_NoMatch_ReturnsFalse()
        {
            var result = await _repository.ExistsByNameAsync(1, 1, "local");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_MatchingNameDifferentScope_ReturnsFalse()
        {
            await Context.AddAsync(CreateOrigin(1, 1, "LOCAL"));
            await Context.SaveChangesAsync();

            var result = await _repository.ExistsByNameAsync(2, 2, "local");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_WithExcludeId_IgnoresTheExcludedRecord()
        {
            var origin = CreateOrigin(1, 1, "LOCAL");
            await Context.AddAsync(origin);
            await Context.SaveChangesAsync();

            var result = await _repository.ExistsByNameAsync(1, 1, "local", excludeId: origin.Id);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidEntity_PersistsToDatabase()
        {
            var origin = CreateOrigin(null, null, "KEETA");

            await _repository.AddAsync(origin);
            await Context.SaveChangesAsync();

            var stored = await Context.Set<OrderOrigin>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == origin.Id);
            stored.Should().NotBeNull();
            stored!.Name.Should().Be("KEETA");
        }

        [Fact]
        public async Task Update_ExistingEntity_MarksEntityAsModified()
        {
            var origin = CreateOrigin(null, null, "LOCAL");
            await Context.AddAsync(origin);
            await Context.SaveChangesAsync();
            Context.Entry(origin).State = EntityState.Detached;

            var tracked = await _repository.GetByIdForUpdateAsync(origin.Id);
            _repository.Update(tracked!);

            Context.Entry(tracked!).State.Should().Be(EntityState.Modified);
        }

        [Fact]
        public async Task Remove_ExistingEntity_MarksAsDeletedAfterSave()
        {
            var origin = CreateOrigin(null, null, "LOCAL");
            await Context.AddAsync(origin);
            await Context.SaveChangesAsync();

            _repository.Remove(origin);
            await Context.SaveChangesAsync();

            var stored = await Context.Set<OrderOrigin>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == origin.Id);
            stored.Should().BeNull();
        }
    }
}
