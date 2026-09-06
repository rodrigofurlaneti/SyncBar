using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AppUserRepositoryTests : RepositoryTestBase
    {
        private readonly AppUserRepository _repository;

        public AppUserRepositoryTests()
        {
            _repository = new AppUserRepository(Context);
        }

        private static AppUser CreateUser(
            long companyId = 1, long? employeeId = null, string userName = "user1", string email = "user1@teste.com") =>
            AppUser.Create(companyId, employeeId, userName, email, "hash").Value;

        private async Task<AppUser> SeedAsync(AppUser user)
        {
            await Context.AddAsync(user);
            await Context.SaveChangesAsync();
            Context.Entry(user).State = EntityState.Detached;
            return user;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedUser()
        {
            var user = await SeedAsync(CreateUser());

            var result = await _repository.GetByIdAsync(user.Id);

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
            var user = await SeedAsync(CreateUser(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(user.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedUser()
        {
            var user = await SeedAsync(CreateUser());

            var result = await _repository.GetByIdForUpdateAsync(user.Id);

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
        public async Task GetByCompanyAsync_MultipleUsers_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateUser(companyId: 5, userName: "user1", email: "u1@teste.com"));
            var second = await SeedAsync(CreateUser(companyId: 5, userName: "user2", email: "u2@teste.com"));
            await SeedAsync(CreateUser(companyId: 6, userName: "user3", email: "u3@teste.com"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_NoUsersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByEmployeeIdsAsync_MatchingActiveUsers_ReturnsUsers()
        {
            var first = await SeedAsync(CreateUser(employeeId: 10, userName: "u1", email: "u1@teste.com"));
            var second = await SeedAsync(CreateUser(employeeId: 20, userName: "u2", email: "u2@teste.com"));
            await SeedAsync(CreateUser(employeeId: 30, userName: "u3", email: "u3@teste.com"));

            var result = await _repository.GetByEmployeeIdsAsync([10, 20]);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByEmployeeIdsAsync_EmptyList_ReturnsEmptyListWithoutQuerying()
        {
            var result = await _repository.GetByEmployeeIdsAsync([]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByEmployeeIdsAsync_DeactivatedUser_IsExcluded()
        {
            var user = await SeedAsync(CreateUser(employeeId: 10));
            var tracked = await _repository.GetByIdForUpdateAsync(user.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByEmployeeIdsAsync([10]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUserNameForUpdateAsync_ExistingActiveUser_ReturnsTrackedUser()
        {
            var user = await SeedAsync(CreateUser(userName: "meu-usuario"));

            var result = await _repository.GetByUserNameForUpdateAsync("meu-usuario");

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByUserNameForUpdateAsync_DeactivatedUser_ReturnsNull()
        {
            var user = await SeedAsync(CreateUser(userName: "usuario-inativo"));
            var tracked = await _repository.GetByIdForUpdateAsync(user.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByUserNameForUpdateAsync("usuario-inativo");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByUserNameForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByUserNameForUpdateAsync("nao-existe");

            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsAsync_MatchingUserName_ReturnsTrue()
        {
            await SeedAsync(CreateUser(userName: "usuario-existente", email: "outro@teste.com"));

            var result = await _repository.ExistsAsync("usuario-existente", "email-diferente@teste.com");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_MatchingEmail_ReturnsTrue()
        {
            await SeedAsync(CreateUser(userName: "outro-usuario", email: "email-existente@teste.com"));

            var result = await _repository.ExistsAsync("usuario-diferente", "email-existente@teste.com");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_NoMatch_ReturnsFalse()
        {
            var result = await _repository.ExistsAsync("nao-existe", "nao-existe@teste.com");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetRoleNamesAsync_UserWithActiveRoles_ReturnsDistinctRoleNames()
        {
            var user = await SeedAsync(CreateUser());
            var role = Role.Create(1, "Administrador", null).Value;
            await Context.AddAsync(role);
            await Context.SaveChangesAsync();
            var userRole = UserRole.Create(1, user.Id, role.Id).Value;
            await Context.AddAsync(userRole);
            await Context.SaveChangesAsync();

            var result = await _repository.GetRoleNamesAsync(user.Id);

            result.Should().ContainSingle().Which.Should().Be("Administrador");
        }

        [Fact]
        public async Task GetRoleNamesAsync_UserWithoutRoles_ReturnsEmptyList()
        {
            var result = await _repository.GetRoleNamesAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetPermissionCodesAsync_UserWithRoleAndPermission_ReturnsDistinctPermissionCodes()
        {
            var user = await SeedAsync(CreateUser());
            var role = Role.Create(1, "Administrador", null).Value;
            var permission = Permission.Create("USERS_MANAGE", "Gerenciar usuários", "Access").Value;
            await Context.AddRangeAsync(role, permission);
            await Context.SaveChangesAsync();
            var userRole = UserRole.Create(1, user.Id, role.Id).Value;
            var rolePermission = RolePermission.Create(role.Id, permission.Id).Value;
            await Context.AddRangeAsync(userRole, rolePermission);
            await Context.SaveChangesAsync();

            var result = await _repository.GetPermissionCodesAsync(user.Id);

            result.Should().ContainSingle().Which.Should().Be("USERS_MANAGE");
        }

        [Fact]
        public async Task GetPermissionCodesAsync_UserWithoutRoles_ReturnsEmptyList()
        {
            var result = await _repository.GetPermissionCodesAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidUser_PersistsToDatabase()
        {
            var user = CreateUser(userName: "novo-usuario", email: "novo@teste.com");

            await _repository.AddAsync(user);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AppUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == "novo-usuario");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
