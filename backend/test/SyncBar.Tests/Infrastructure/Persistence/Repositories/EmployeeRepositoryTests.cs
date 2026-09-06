using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class EmployeeRepositoryTests : RepositoryTestBase
    {
        private readonly EmployeeRepository _repository;

        public EmployeeRepositoryTests()
        {
            _repository = new EmployeeRepository(Context);
        }

        private static Employee CreateEmployee(long branchId = 1, string cpf = "11111111111") =>
            Employee.Create(branchId, 1, "Funcionário 1", cpf, null, null, DateTime.UtcNow, null, null).Value;

        private async Task<Employee> SeedAsync(Employee employee)
        {
            await Context.AddAsync(employee);
            await Context.SaveChangesAsync();
            Context.Entry(employee).State = EntityState.Detached;
            return employee;
        }

        // Employee tem filtro global de tenant por filial (via subquery Branchs.Any(...)). Com
        // TenantService.CompanyId nulo (padrão do fake), o filtro é ignorado e nenhuma Branch real
        // precisa existir — só os testes que exercitam o filtro (abaixo) semeiam uma Branch de verdade.

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedEmployee()
        {
            var employee = await SeedAsync(CreateEmployee());

            var result = await _repository.GetByIdAsync(employee.Id);

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
        public async Task GetByIdAsync_SameTenantBranch_ReturnsEmployee()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            var employee = await SeedAsync(CreateEmployee(branchId: branch.Id));
            TenantService.CompanyId = 5;

            var result = await _repository.GetByIdAsync(employee.Id);

            result.Should().NotBeNull();
        }

        [Fact]
        public async Task GetByIdAsync_DifferentTenantBranch_ReturnsNull()
        {
            var branch = Branch.Create(5, "Matriz", null, null, null, null, null, null, null, null).Value;
            await Context.AddAsync(branch);
            await Context.SaveChangesAsync();
            var employee = await SeedAsync(CreateEmployee(branchId: branch.Id));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(employee.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedEmployee()
        {
            var employee = await SeedAsync(CreateEmployee());

            var result = await _repository.GetByIdForUpdateAsync(employee.Id);

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
        public async Task ExistsByCpfAsync_ActiveEmployeeWithCpf_ReturnsTrue()
        {
            await SeedAsync(CreateEmployee(cpf: "22222222222"));

            var result = await _repository.ExistsByCpfAsync("22222222222");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByCpfAsync_DeactivatedEmployee_ReturnsFalse()
        {
            var employee = await SeedAsync(CreateEmployee(cpf: "33333333333"));
            var tracked = await _repository.GetByIdForUpdateAsync(employee.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.ExistsByCpfAsync("33333333333");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByCpfAsync_NonExistingCpf_ReturnsFalse()
        {
            var result = await _repository.ExistsByCpfAsync("00000000000");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveEmployees_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateEmployee(branchId: 10, cpf: "11111111111"));
            var second = await SeedAsync(CreateEmployee(branchId: 10, cpf: "22222222222"));
            await SeedAsync(CreateEmployee(branchId: 20, cpf: "33333333333"));

            var result = await _repository.GetByBranchAsync(10);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_InactiveEmployee_IsExcluded()
        {
            var employee = await SeedAsync(CreateEmployee(branchId: 10));
            var tracked = await _repository.GetByIdForUpdateAsync(employee.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByBranchAsync(10);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchAsync_NoEmployeesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidEmployee_PersistsToDatabase()
        {
            var employee = CreateEmployee(cpf: "44444444444");

            await _repository.AddAsync(employee);
            await Context.SaveChangesAsync();

            var persisted = await Context.Employees
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Cpf == "44444444444");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
