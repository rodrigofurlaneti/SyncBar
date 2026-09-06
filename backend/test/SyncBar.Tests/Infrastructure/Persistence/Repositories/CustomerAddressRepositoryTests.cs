using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CustomerAddressRepositoryTests : RepositoryTestBase
    {
        private readonly CustomerAddressRepository _repository;

        public CustomerAddressRepositoryTests()
        {
            _repository = new CustomerAddressRepository(Context);
        }

        private static CustomerAddress CreateAddress(
            long companyId = 1, long? branchId = null, long? customerId = null) =>
            CustomerAddress.Create(companyId, branchId, customerId, "Rua A", "100", "Casa 2", "12345000").Value;

        private async Task<CustomerAddress> SeedAsync(CustomerAddress address)
        {
            await Context.AddAsync(address);
            await Context.SaveChangesAsync();
            Context.Entry(address).State = EntityState.Detached;
            return address;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingActiveAddress_ReturnsAddress()
        {
            var address = await SeedAsync(CreateAddress());

            var result = await _repository.GetByIdAsync(address.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(address.Id);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCustomerIdAsync_MultipleAddresses_ReturnsOrderedByCreatedAtDescending()
        {
            var older = await SeedAsync(CreateAddress(customerId: 5));
            await Task.Delay(10);
            var newer = await SeedAsync(CreateAddress(customerId: 5));

            var result = (await _repository.GetByCustomerIdAsync(5)).ToList();

            result.Should().HaveCount(2);
            result[0].Id.Should().Be(newer.Id);
            result[1].Id.Should().Be(older.Id);
        }

        [Fact]
        public async Task GetByCustomerIdAsync_NoAddressesForCustomer_ReturnsEmptyList()
        {
            var result = await _repository.GetByCustomerIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyIdAsync_MultipleAddresses_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateAddress(companyId: 5));
            var second = await SeedAsync(CreateAddress(companyId: 5));
            await SeedAsync(CreateAddress(companyId: 6));

            var result = (await _repository.GetByCompanyIdAsync(5)).ToList();

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyIdAsync_NoAddressesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBranchIdAsync_MultipleAddresses_ReturnsAllForBranch()
        {
            var address = await SeedAsync(CreateAddress(branchId: 10));
            await SeedAsync(CreateAddress(branchId: 20));

            var result = (await _repository.GetByBranchIdAsync(10)).ToList();

            result.Should().ContainSingle(x => x.Id == address.Id);
        }

        [Fact]
        public async Task GetByBranchIdAsync_NoAddressesForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByLastOrderIdAsync_MatchingOrder_ReturnsAddress()
        {
            var address = CreateAddress();
            address.RegisterOrderUsage(orderId: 555);
            await Context.AddAsync(address);
            await Context.SaveChangesAsync();

            var result = (await _repository.GetByLastOrderIdAsync(555)).ToList();

            result.Should().ContainSingle(x => x.Id == address.Id);
        }

        [Fact]
        public async Task GetByLastOrderIdAsync_NoMatch_ReturnsEmptyList()
        {
            var result = await _repository.GetByLastOrderIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidAddress_PersistsToDatabase()
        {
            var address = CreateAddress(companyId: 7);

            await _repository.AddAsync(address);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerAddress>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == 7);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task UpdateAsync_ExistingAddress_PersistsChanges()
        {
            var address = await SeedAsync(CreateAddress());
            var tracked = await Context.Set<CustomerAddress>().FirstAsync(x => x.Id == address.Id);
            tracked.UpdateDetails("Rua Nova", "200", "Fundos", "54321000");

            await _repository.UpdateAsync(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<CustomerAddress>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == address.Id);

            persisted.Street.Should().Be("Rua Nova");
        }

        [Fact]
        public async Task RemoveAsync_ExistingAddress_DeactivatesAndExcludesFromReads()
        {
            var address = await SeedAsync(CreateAddress());

            await _repository.RemoveAsync(address.Id);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByIdAsync(address.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task RemoveAsync_NonExistingAddress_DoesNothing()
        {
            await _repository.RemoveAsync(999);
            var act = async () => await Context.SaveChangesAsync();

            await act.Should().NotThrowAsync();
        }
    }
}
