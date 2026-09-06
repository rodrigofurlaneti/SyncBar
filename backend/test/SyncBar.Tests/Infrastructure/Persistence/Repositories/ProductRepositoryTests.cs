using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ProductRepositoryTests : RepositoryTestBase
    {
        private readonly ProductRepository _repository;

        public ProductRepositoryTests()
        {
            _repository = new ProductRepository(Context);
        }

        private static Product CreateProduct(
            long companyId = 1, string name = "Produto 1", string? barcode = null, decimal salePrice = 10m) =>
            Product.Create(companyId, 1, 1, name, null, barcode, salePrice, null, false, null).Value;

        private async Task<Product> SeedAsync(Product product)
        {
            await Context.AddAsync(product);
            await Context.SaveChangesAsync();
            Context.Entry(product).State = EntityState.Detached;
            return product;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedProduct()
        {
            var product = await SeedAsync(CreateProduct());

            var result = await _repository.GetByIdAsync(product.Id);

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
            var product = await SeedAsync(CreateProduct(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(product.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedProduct()
        {
            var product = await SeedAsync(CreateProduct());

            var result = await _repository.GetByIdForUpdateAsync(product.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveProducts_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateProduct(companyId: 5, name: "P1"));
            var second = await SeedAsync(CreateProduct(companyId: 5, name: "P2"));
            await SeedAsync(CreateProduct(companyId: 6, name: "P3"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveProduct_IsExcluded()
        {
            var product = await SeedAsync(CreateProduct(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(product.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoProductsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetAllByCompanyAsync_IncludesInactiveProducts()
        {
            var active = await SeedAsync(CreateProduct(companyId: 5, name: "Ativo"));
            var inactive = await SeedAsync(CreateProduct(companyId: 5, name: "Inativo"));
            var tracked = await _repository.GetByIdForUpdateAsync(inactive.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetAllByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([active.Id, inactive.Id]);
        }

        [Fact]
        public async Task GetAllByCompanyAsync_NoProductsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetAllByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_MatchingIds_ReturnsProducts()
        {
            var first = await SeedAsync(CreateProduct(name: "P1"));
            var second = await SeedAsync(CreateProduct(name: "P2"));
            await SeedAsync(CreateProduct(name: "P3"));

            var result = await _repository.GetByIdsAsync([first.Id, second.Id]);

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetByIdsAsync_NoMatchingIds_ReturnsEmptyList()
        {
            var result = await _repository.GetByIdsAsync([999]);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByBarcodeAsync_ExistingActiveBarcode_ReturnsProduct()
        {
            var product = await SeedAsync(CreateProduct(companyId: 5, barcode: "7891234567890"));

            var result = await _repository.GetByBarcodeAsync(5, "7891234567890");

            result.Should().NotBeNull();
            result!.Id.Should().Be(product.Id);
        }

        [Fact]
        public async Task GetByBarcodeAsync_DifferentCompany_ReturnsNull()
        {
            await SeedAsync(CreateProduct(companyId: 5, barcode: "7891234567890"));

            var result = await _repository.GetByBarcodeAsync(6, "7891234567890");

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBarcodeAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByBarcodeAsync(999, "0000000000000");

            result.Should().BeNull();
        }

        [Fact]
        public async Task AddAsync_ValidProduct_PersistsToDatabase()
        {
            var product = CreateProduct(companyId: 7, name: "Produto Novo");

            await _repository.AddAsync(product);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<Product>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Produto Novo");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
