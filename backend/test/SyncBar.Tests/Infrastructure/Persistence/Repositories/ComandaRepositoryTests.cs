using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ComandaRepositoryTests : RepositoryTestBase
    {
        private readonly ComandaRepository _repository;

        public ComandaRepositoryTests()
        {
            _repository = new ComandaRepository(Context);
        }

        private static Comanda CreateComanda(long branchId = 1, string code = "001") =>
            Comanda.Create(branchId, 1, code).Value;

        private async Task<Comanda> SeedAsync(Comanda comanda)
        {
            await Context.AddAsync(comanda);
            await Context.SaveChangesAsync();
            Context.Entry(comanda).State = EntityState.Detached;
            return comanda;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedComanda()
        {
            var comanda = await SeedAsync(CreateComanda());

            var result = await _repository.GetByIdAsync(comanda.Id);

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
            var comanda = await SeedAsync(CreateComanda(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(comanda.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedComanda()
        {
            var comanda = await SeedAsync(CreateComanda());

            var result = await _repository.GetByIdForUpdateAsync(comanda.Id);

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
        public async Task GetByCodeAsync_ExistingActiveCode_ReturnsComanda()
        {
            var comanda = await SeedAsync(CreateComanda(branchId: 5, code: "042"));

            var result = await _repository.GetByCodeAsync(5, "042");

            result.Should().NotBeNull();
            result!.Id.Should().Be(comanda.Id);
        }

        [Fact]
        public async Task GetByCodeAsync_DifferentBranch_ReturnsNull()
        {
            await SeedAsync(CreateComanda(branchId: 5, code: "042"));

            var result = await _repository.GetByCodeAsync(6, "042");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCodeAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByCodeAsync(999, "999");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveComandas_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateComanda(branchId: 5, code: "001"));
            var second = await SeedAsync(CreateComanda(branchId: 5, code: "002"));
            await SeedAsync(CreateComanda(branchId: 6, code: "003"));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveComanda_IsExcluded()
        {
            var comanda = await SeedAsync(CreateComanda(branchId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(comanda.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoComandasForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidComanda_PersistsToDatabase()
        {
            var comanda = CreateComanda(branchId: 7, code: "999");

            await _repository.AddAsync(comanda);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Comanda>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "999");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
