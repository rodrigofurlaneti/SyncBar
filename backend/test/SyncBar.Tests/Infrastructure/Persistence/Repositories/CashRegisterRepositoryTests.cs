using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CashRegisterRepositoryTests : RepositoryTestBase
    {
        private readonly CashRegisterRepository _repository;

        public CashRegisterRepositoryTests()
        {
            _repository = new CashRegisterRepository(Context);
        }

        private static CashRegister CreateRegister(long branchId = 1, string name = "Caixa 1") =>
            CashRegister.Create(branchId, name).Value;

        private async Task<CashRegister> SeedAsync(CashRegister register)
        {
            await Context.AddAsync(register);
            await Context.SaveChangesAsync();
            Context.Entry(register).State = EntityState.Detached;
            return register;
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveRegisters_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateRegister(branchId: 5, name: "R1"));
            var second = await SeedAsync(CreateRegister(branchId: 5, name: "R2"));
            await SeedAsync(CreateRegister(branchId: 6, name: "R3"));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveRegister_IsExcluded()
        {
            var register = await SeedAsync(CreateRegister(branchId: 5));
            register.Deactivate();
            Context.Update(register);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoRegistersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_DifferentTenantBranch_ReturnsEmptyList()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateRegister(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByBranchAsync(branch.Id);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidRegister_PersistsToDatabase()
        {
            var register = CreateRegister(branchId: 7, name: "Caixa Novo");

            await _repository.AddAsync(register);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CashRegister>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Caixa Novo");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
