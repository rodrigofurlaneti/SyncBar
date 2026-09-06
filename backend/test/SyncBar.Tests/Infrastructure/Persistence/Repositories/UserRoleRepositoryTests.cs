using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class UserRoleRepositoryTests : RepositoryTestBase
    {
        private readonly UserRoleRepository _repository;

        public UserRoleRepositoryTests()
        {
            _repository = new UserRoleRepository(Context);
        }

        private static UserRole CreateUserRole(long companyId = 1, long appUserId = 1, long roleId = 1) =>
            UserRole.Create(companyId, appUserId, roleId).Value;

        private async Task<UserRole> SeedAsync(UserRole userRole)
        {
            await Context.AddAsync(userRole);
            await Context.SaveChangesAsync();
            Context.Entry(userRole).State = EntityState.Detached;
            return userRole;
        }

        [Fact]
        public async Task GetByUserForUpdateAsync_ExistingLinks_ReturnsTrackedEntities()
        {
            var userRole = await SeedAsync(CreateUserRole(appUserId: 5));

            var result = await _repository.GetByUserForUpdateAsync(5);

            result.Should().ContainSingle(x => x.Id == userRole.Id);
            Context.Entry(result.Single()).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByUserForUpdateAsync_IncludesInactiveLinks()
        {
            var userRole = await SeedAsync(CreateUserRole(appUserId: 5));
            var tracked = (await _repository.GetByUserForUpdateAsync(5)).Single();
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<UserRole>().AsNoTracking().FirstAsync(x => x.Id == userRole.Id);

            persisted.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByUserForUpdateAsync_NoLinks_ReturnsEmptyList()
        {
            var result = await _repository.GetByUserForUpdateAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUsersAsync_MultipleActiveLinks_ReturnsMatchingUsers()
        {
            var first = await SeedAsync(CreateUserRole(appUserId: 5));
            var second = await SeedAsync(CreateUserRole(appUserId: 6));
            await SeedAsync(CreateUserRole(appUserId: 7));

            var result = await _repository.GetByUsersAsync([5, 6]);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByUsersAsync_DeactivatedLink_IsExcluded()
        {
            var userRole = await SeedAsync(CreateUserRole(appUserId: 5));
            var tracked = (await _repository.GetByUserForUpdateAsync(5)).Single();
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByUsersAsync([5]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUsersAsync_NoMatchingUsers_ReturnsEmptyList()
        {
            var result = await _repository.GetByUsersAsync([999]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidUserRole_PersistsToDatabase()
        {
            var userRole = CreateUserRole(appUserId: 10, roleId: 20);

            await _repository.AddAsync(userRole);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<UserRole>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == 10 && x.RoleId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
