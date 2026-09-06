using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class SupplierRepositoryTests : RepositoryTestBase
    {
        private readonly SupplierRepository _repository;

        public SupplierRepositoryTests()
        {
            _repository = new SupplierRepository(Context);
        }

        private static Supplier CreateSupplier(long companyId = 1, string? tradeName = "Distribuidora") =>
            Supplier.Create(companyId, "Razão Social", tradeName, null, null, null).Value;

        private async Task<Supplier> SeedAsync(Supplier supplier)
        {
            await Context.AddAsync(supplier);
            await Context.SaveChangesAsync();
            Context.Entry(supplier).State = EntityState.Detached;
            return supplier;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedSupplier()
        {
            var supplier = await SeedAsync(CreateSupplier());

            var result = await _repository.GetByIdAsync(supplier.Id);

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
            var supplier = await SeedAsync(CreateSupplier(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(supplier.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedSupplier()
        {
            var supplier = await SeedAsync(CreateSupplier());

            var result = await _repository.GetByIdForUpdateAsync(supplier.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveSuppliers_ReturnsOrderedByTradeName()
        {
            var zebra = await SeedAsync(CreateSupplier(companyId: 5, tradeName: "Zebra"));
            var alpha = await SeedAsync(CreateSupplier(companyId: 5, tradeName: "Alpha"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.ElementAt(0).Id.Should().Be(alpha.Id);
            result.ElementAt(1).Id.Should().Be(zebra.Id);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveSupplier_IsExcluded()
        {
            var supplier = await SeedAsync(CreateSupplier(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(supplier.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoSuppliersForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_DifferentTenant_ReturnsEmptyList()
        {
            await SeedAsync(CreateSupplier(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidSupplier_PersistsToDatabase()
        {
            var supplier = CreateSupplier(companyId: 7, tradeName: "Novo Fornecedor");

            await _repository.AddAsync(supplier);
            await Context.SaveChangesAsync();

            var persisted = await Context.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.TradeName == "Novo Fornecedor");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
