using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AsaasIntegrationCustomerRepositoryTests : RepositoryTestBase
    {
        private readonly AsaasIntegrationCustomerRepository _repository;

        public AsaasIntegrationCustomerRepositoryTests()
        {
            _repository = new AsaasIntegrationCustomerRepository(Context);
        }

        // AsaasIntegrationCustomer não tem método de desativação (IsActive sempre true a partir do
        // Create) — o filtro IsActive nas queries não tem cenário "inativo" real pra testar via API
        // pública de domínio, então os testes cobrem apenas o comportamento alcançável.
        private static AsaasIntegrationCustomer CreateCustomer(
            long customerId = 1, long companyId = 1, string asaasCustomerId = "cus_1") =>
            AsaasIntegrationCustomer.Create(customerId, companyId, asaasCustomerId).Value;

        private async Task<AsaasIntegrationCustomer> SeedAsync(AsaasIntegrationCustomer customer)
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
            result!.Id.Should().Be(customer.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCustomerIdAndCompanyIdAsync_MatchingBoth_ReturnsCustomer()
        {
            var customer = await SeedAsync(CreateCustomer(customerId: 10, companyId: 5));

            var result = await _repository.GetByCustomerIdAndCompanyIdAsync(10, 5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(customer.Id);
        }

        [Fact]
        public async Task GetByCustomerIdAndCompanyIdAsync_MatchingCustomerDifferentCompany_ReturnsNull()
        {
            await SeedAsync(CreateCustomer(customerId: 10, companyId: 5));

            var result = await _repository.GetByCustomerIdAndCompanyIdAsync(10, 6);

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
        public async Task GetByCustomerIdAndCompanyIdForUpdateAsync_MatchingBoth_ReturnsTrackedCustomer()
        {
            var customer = await SeedAsync(CreateCustomer(customerId: 10, companyId: 5));

            var result = await _repository.GetByCustomerIdAndCompanyIdForUpdateAsync(10, 5);

            result.Should().NotBeNull();
            result!.Id.Should().Be(customer.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByCustomerIdAndCompanyIdForUpdateAsync_NoMatch_ReturnsNull()
        {
            var result = await _repository.GetByCustomerIdAndCompanyIdForUpdateAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByAsaasCustomerIdAsync_ExistingAsaasCustomerId_ReturnsCustomer()
        {
            var customer = await SeedAsync(CreateCustomer(asaasCustomerId: "cus_abc"));

            var result = await _repository.GetByAsaasCustomerIdAsync("cus_abc");

            result.Should().NotBeNull();
            result!.Id.Should().Be(customer.Id);
        }

        [Fact]
        public async Task GetByAsaasCustomerIdAsync_NonExistingAsaasCustomerId_ReturnsNull()
        {
            var result = await _repository.GetByAsaasCustomerIdAsync("cus_missing");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_MultipleCustomersSameCompany_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateCustomer(customerId: 1, companyId: 5, asaasCustomerId: "cus_1"));
            var second = await SeedAsync(CreateCustomer(customerId: 2, companyId: 5, asaasCustomerId: "cus_2"));
            await SeedAsync(CreateCustomer(customerId: 3, companyId: 6, asaasCustomerId: "cus_3"));

            var result = await _repository.GetAllByCompanyIdAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetAllByCompanyIdAsync_NoCustomersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByCompanyIdAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task ExistsAsync_MatchingCustomerAndCompany_ReturnsTrue()
        {
            await SeedAsync(CreateCustomer(customerId: 10, companyId: 5));

            var result = await _repository.ExistsAsync(10, 5);

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsAsync_NoMatch_ReturnsFalse()
        {
            var result = await _repository.ExistsAsync(999, 999);

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidCustomer_PersistsToDatabase()
        {
            var customer = CreateCustomer(customerId: 20, companyId: 7);

            await _repository.AddAsync(customer);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationCustomer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.CustomerId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task Update_ExistingCustomer_PersistsChanges()
        {
            var customer = await SeedAsync(CreateCustomer(asaasCustomerId: "cus_old"));
            var tracked = await _repository.GetByIdForUpdateAsync(customer.Id);
            tracked!.UpdateAsaasCustomerId("cus_new");

            _repository.Update(tracked);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationCustomer>()
                .AsNoTracking()
                .FirstAsync(x => x.Id == customer.Id);

            persisted.AsaasCustomerId.Should().Be("cus_new");
        }

        [Fact]
        public async Task Delete_ExistingCustomer_RemovesFromDatabase()
        {
            var customer = await SeedAsync(CreateCustomer());
            var tracked = await _repository.GetByIdForUpdateAsync(customer.Id);

            _repository.Delete(tracked!);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AsaasIntegrationCustomer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == customer.Id);

            persisted.Should().BeNull();
        }
    }
}
