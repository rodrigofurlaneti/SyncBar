using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class DiningTableRepositoryTests : RepositoryTestBase
    {
        private readonly DiningTableRepository _repository;

        public DiningTableRepositoryTests()
        {
            _repository = new DiningTableRepository(Context);
        }

        private static DiningTable CreateTable(long branchId = 1, int number = 1) =>
            DiningTable.Create(branchId, 1, number, 4).Value;

        private async Task<DiningTable> SeedAsync(DiningTable table)
        {
            await Context.AddAsync(table);
            await Context.SaveChangesAsync();
            Context.Entry(table).State = EntityState.Detached;
            return table;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedTable()
        {
            var table = await SeedAsync(CreateTable());

            var result = await _repository.GetByIdAsync(table.Id);

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
        public async Task GetByIdAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            var table = await SeedAsync(CreateTable(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(table.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedTable()
        {
            var table = await SeedAsync(CreateTable());

            var result = await _repository.GetByIdForUpdateAsync(table.Id);

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
        public async Task GetByBranchAsync_MultipleActiveTables_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateTable(branchId: 5, number: 1));
            var second = await SeedAsync(CreateTable(branchId: 5, number: 2));
            await SeedAsync(CreateTable(branchId: 6, number: 3));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveTable_IsExcluded()
        {
            var table = await SeedAsync(CreateTable(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(table.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoTablesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByQrTokenAsync_ExistingActiveToken_ReturnsTable()
        {
            var table = await SeedAsync(CreateTable());
            var tracked = await _repository.GetByIdForUpdateAsync(table.Id);
            var token = tracked!.GenerateQrToken();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByQrTokenAsync(token);

            result.Should().NotBeNull();
            result!.Id.Should().Be(table.Id);
        }

        [Fact]
        public async Task GetByQrTokenAsync_NonExistingToken_ReturnsNull()
        {
            var result = await _repository.GetByQrTokenAsync(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidTable_PersistsToDatabase()
        {
            var table = CreateTable(branchId: 7, number: 99);

            await _repository.AddAsync(table);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningTable>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Number == 99);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingTable_PersistsChanges()
        {
            var table = await SeedAsync(CreateTable());
            var tracked = await _repository.GetByIdForUpdateAsync(table.Id);
            tracked!.Deactivate();

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<DiningTable>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == table.Id);

            persisted.IsActive.Should().BeFalse();
        }
    }
}
