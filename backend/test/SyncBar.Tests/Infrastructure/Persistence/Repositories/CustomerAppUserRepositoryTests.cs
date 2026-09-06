using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CustomerAppUserRepositoryTests : RepositoryTestBase
    {
        private readonly CustomerAppUserRepository _repository;

        public CustomerAppUserRepositoryTests()
        {
            _repository = new CustomerAppUserRepository(Context);
        }

        private static CustomerAppUser CreateUser(
            long companyId = 1, long? branchId = null, long? customerId = null,
            string userName = "user1", string email = "user1@teste.com") =>
            CustomerAppUser.Create(companyId, branchId, customerId, userName, email, "hash").Value;

        private async Task<CustomerAppUser> SeedAsync(CustomerAppUser user)
        {
            await Context.AddAsync(user);
            await Context.SaveChangesAsync();
            Context.Entry(user).State = EntityState.Detached;
            return user;
        }

        [Fact]
        public async Task GetByCustomerId_MultipleActiveUsers_ReturnsAllForCustomer()
        {
            var first = await SeedAsync(CreateUser(customerId: 5, userName: "u1", email: "u1@teste.com"));
            await SeedAsync(CreateUser(customerId: 6, userName: "u2", email: "u2@teste.com"));

            var result = (await _repository.GetByCustomerId(5)).ToList();

            result.Should().ContainSingle(x => x.Id == first.Id);
        }

        [Fact]
        public async Task GetByCustomerId_NoUsersForCustomer_ReturnsEmptyList()
        {
            var result = await _repository.GetByCustomerId(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchId_MultipleActiveUsers_ReturnsAllForBranch()
        {
            var first = await SeedAsync(CreateUser(branchId: 10, userName: "u1", email: "u1@teste.com"));
            await SeedAsync(CreateUser(branchId: 20, userName: "u2", email: "u2@teste.com"));

            var result = (await _repository.GetByBranchId(10)).ToList();

            result.Should().ContainSingle(x => x.Id == first.Id);
        }

        [Fact]
        public async Task GetByBranchId_NoUsersForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchId(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyId_MultipleActiveUsers_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateUser(companyId: 5, userName: "u1", email: "u1@teste.com"));
            await SeedAsync(CreateUser(companyId: 6, userName: "u2", email: "u2@teste.com"));

            var result = (await _repository.GetByCompanyId(5)).ToList();

            result.Should().ContainSingle(x => x.Id == first.Id);
        }

        [Fact]
        public async Task GetByCompanyId_NoUsersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyId(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsTrackedUser()
        {
            var user = await SeedAsync(CreateUser());

            var result = await _repository.GetByIdAsync(user.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailForUpdateAsync_ExistingActiveUser_ReturnsTrackedUser()
        {
            var user = await SeedAsync(CreateUser(companyId: 5, email: "specific@teste.com"));

            var result = await _repository.GetByEmailForUpdateAsync("specific@teste.com", 5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(user.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByEmailForUpdateAsync_DifferentCompany_ReturnsNull()
        {
            await SeedAsync(CreateUser(companyId: 5, email: "specific@teste.com"));

            var result = await _repository.GetByEmailForUpdateAsync("specific@teste.com", 6);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByEmailForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByEmailForUpdateAsync("missing@teste.com", 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidUser_PersistsToDatabase()
        {
            var user = CreateUser(userName: "novo-usuario", email: "novo@teste.com");

            await _repository.AddAsync(user);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerAppUser>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserName == "novo-usuario");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingUser_PersistsChanges()
        {
            var user = await SeedAsync(CreateUser());
            var tracked = await Context.Set<CustomerAppUser>().FirstAsync(x => x.Id == user.Id);
            tracked.Deactivate();

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerAppUser>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == user.Id);

            persisted.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_ExistingUser_DeactivatesUser()
        {
            var user = await SeedAsync(CreateUser());

            await _repository.RemoveAsync(user.Id);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerAppUser>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == user.Id);

            persisted.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task RemoveAsync_NonExistingUser_DoesNothing()
        {
            await _repository.RemoveAsync(999);
            var act = async () => await Context.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
