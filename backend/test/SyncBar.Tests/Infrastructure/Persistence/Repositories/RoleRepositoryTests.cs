using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class RoleRepositoryTests : RepositoryTestBase
    {
        private readonly RoleRepository _repository;

        public RoleRepositoryTests()
        {
            _repository = new RoleRepository(Context);
        }

        private static Role CreateRole(long companyId = 1, string name = "Administrador") =>
            Role.Create(companyId, name, null).Value;

        private async Task<Role> SeedAsync(Role role)
        {
            await Context.AddAsync(role);
            await Context.SaveChangesAsync();
            Context.Entry(role).State = EntityState.Detached;
            return role;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedRole()
        {
            var role = await SeedAsync(CreateRole());

            var result = await _repository.GetByIdAsync(role.Id);

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
        public async Task GetByIdAsync_DifferentTenant_ReturnsNull()
        {
            var role = await SeedAsync(CreateRole(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(role.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_ExistingActiveRole_ReturnsRole()
        {
            var role = await SeedAsync(CreateRole(companyId: 5, name: "Gerente"));

            var result = await _repository.GetByNameAsync(5, "Gerente");

            result.Should().NotBeNull();
            result!.Id.Should().Be(role.Id);
        }

        [Fact]
        public async Task GetByNameAsync_DifferentCompany_ReturnsNull()
        {
            await SeedAsync(CreateRole(companyId: 5, name: "Gerente"));

            var result = await _repository.GetByNameAsync(6, "Gerente");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByNameAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByNameAsync(999, "Não existe");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_MultipleActiveRoles_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateRole(companyId: 5, name: "Admin"));
            var second = await SeedAsync(CreateRole(companyId: 5, name: "Gerente"));
            await SeedAsync(CreateRole(companyId: 6, name: "Outro"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_NoRolesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsByNameAsync_ExistingActiveRole_ReturnsTrue()
        {
            await SeedAsync(CreateRole(companyId: 5, name: "Supervisor"));

            var result = await _repository.ExistsByNameAsync(5, "Supervisor");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByNameAsync_DifferentCompany_ReturnsFalse()
        {
            await SeedAsync(CreateRole(companyId: 5, name: "Supervisor"));

            var result = await _repository.ExistsByNameAsync(6, "Supervisor");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task ExistsByNameAsync_NonExisting_ReturnsFalse()
        {
            var result = await _repository.ExistsByNameAsync(999, "Não existe");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidRole_PersistsToDatabase()
        {
            var role = CreateRole(companyId: 7, name: "Novo Cargo");

            await _repository.AddAsync(role);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Role>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Novo Cargo");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
