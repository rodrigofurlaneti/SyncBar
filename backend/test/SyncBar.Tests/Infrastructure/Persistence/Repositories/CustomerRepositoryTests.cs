using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CustomerRepositoryTests : RepositoryTestBase
    {
        private readonly CustomerRepository _repository;

        public CustomerRepositoryTests()
        {
            _repository = new CustomerRepository(Context);
        }

        private static Customer CreateCustomer(
            long companyId = 1, string name = "Cliente 1", string? phone = null) =>
            Customer.Create(companyId, name, phone, null, null).Value;

        private async Task<Customer> SeedAsync(Customer customer)
        {
            await Context.AddAsync(customer);
            await Context.SaveChangesAsync();
            Context.Entry(customer).State = EntityState.Detached;
            return customer;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedCustomer()
        {
            var customer = await SeedAsync(CreateCustomer());

            var result = await _repository.GetByIdAsync(customer.Id);

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
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedCustomer()
        {
            var customer = await SeedAsync(CreateCustomer());

            var result = await _repository.GetByIdForUpdateAsync(customer.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveCustomers_ReturnsOrderedByName()
        {
            var zebra = await SeedAsync(CreateCustomer(companyId: 5, name: "Zebra"));
            var alpha = await SeedAsync(CreateCustomer(companyId: 5, name: "Alpha"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(alpha.Id);
            result.ElementAt(1).Id.Should().Be(zebra.Id);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveCustomer_IsExcluded()
        {
            var customer = await SeedAsync(CreateCustomer(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(customer.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoCustomersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_MatchingName_ReturnsCustomer()
        {
            var customer = await SeedAsync(CreateCustomer(companyId: 5, name: "João da Silva"));

            var result = await _repository.SearchAsync(5, "João");

            result.Should().ContainSingle(x => x.Id == customer.Id);
        }

        [Fact]
        public async Task SearchAsync_MatchingPhone_ReturnsCustomer()
        {
            var customer = await SeedAsync(CreateCustomer(companyId: 5, name: "Maria", phone: "11999998888"));

            var result = await _repository.SearchAsync(5, "999998888");

            result.Should().ContainSingle(x => x.Id == customer.Id);
        }

        [Fact]
        public async Task SearchAsync_NoMatch_ReturnsEmptyList()
        {
            await SeedAsync(CreateCustomer(companyId: 5, name: "Maria"));

            var result = await _repository.SearchAsync(5, "Não Existe");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task SearchAsync_DifferentCompany_ReturnsEmptyList()
        {
            await SeedAsync(CreateCustomer(companyId: 5, name: "Maria"));

            var result = await _repository.SearchAsync(6, "Maria");

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidCustomer_PersistsToDatabase()
        {
            var customer = CreateCustomer(companyId: 7, name: "Novo Cliente");

            await _repository.AddAsync(customer);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Customer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Novo Cliente");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
