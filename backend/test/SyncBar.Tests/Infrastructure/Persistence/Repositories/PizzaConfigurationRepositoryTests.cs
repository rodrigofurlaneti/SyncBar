using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class PizzaConfigurationRepositoryTests : RepositoryTestBase
    {
        private readonly PizzaConfigurationRepository _repository;

        public PizzaConfigurationRepositoryTests()
        {
            _repository = new PizzaConfigurationRepository(Context);
        }

        private static PizzaConfiguration CreateConfiguration(long productId = 1)
        {
            var configuration = PizzaConfiguration.Create(productId).Value;
            configuration.AddSize("Grande", 8, 2, 0);
            configuration.AddCrust("Tradicional", 0m, 0);
            configuration.AddEdge("Catupiry", 5m, 0);
            var size = configuration.Sizes.Single();
            configuration.SetFlavorPrice(pizzaFlavorId: 1, pizzaSizeId: size.Id, price: 30m);
            return configuration;
        }

        private async Task<PizzaConfiguration> SeedAsync(PizzaConfiguration configuration)
        {
            await Context.AddAsync(configuration);
            await Context.SaveChangesAsync();
            Context.Entry(configuration).State = EntityState.Detached;
            return configuration;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedConfigurationWithChildren()
        {
            var configuration = await SeedAsync(CreateConfiguration());

            var result = await _repository.GetByIdAsync(configuration.Id);

            result.Should().NotBeNull();
            result!.Sizes.Should().ContainSingle();
            result.Crusts.Should().ContainSingle();
            result.Edges.Should().ContainSingle();
            result.FlavorPrices.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedConfigurationWithChildren()
        {
            var configuration = await SeedAsync(CreateConfiguration());

            var result = await _repository.GetByIdForUpdateAsync(configuration.Id);

            result.Should().NotBeNull();
            result!.Sizes.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByProductIdAsync_ExistingActiveConfiguration_ReturnsConfiguration()
        {
            var configuration = await SeedAsync(CreateConfiguration(productId: 42));

            var result = await _repository.GetByProductIdAsync(42);

            result.Should().NotBeNull();
            result!.Id.Should().Be(configuration.Id);
        }

        [Fact]
        public async Task GetByProductIdAsync_DeactivatedConfiguration_ReturnsNull()
        {
            var configuration = await SeedAsync(CreateConfiguration(productId: 42));
            var tracked = await _repository.GetByIdForUpdateAsync(configuration.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByProductIdAsync(42);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByProductIdAsync_NoConfigurationForProduct_ReturnsNull()
        {
            var result = await _repository.GetByProductIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByProductIdForUpdateAsync_ExistingActiveConfiguration_ReturnsTrackedConfiguration()
        {
            var configuration = await SeedAsync(CreateConfiguration(productId: 42));

            var result = await _repository.GetByProductIdForUpdateAsync(42);

            result.Should().NotBeNull();
            result!.Id.Should().Be(configuration.Id);
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByProductIdForUpdateAsync_NoConfiguration_ReturnsNull()
        {
            var result = await _repository.GetByProductIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_ProductBelongsToCompany_ReturnsConfiguration()
        {
            var product = Product.Create(5, 1, 1, "Pizza Grande", null, null, 50m, null, false, null).Value;
            await Context.AddAsync(product);
            await Context.SaveChangesAsync();
            var configuration = await SeedAsync(CreateConfiguration(productId: product.Id));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().ContainSingle(x => x.Id == configuration.Id);
        }

        [Fact]
        public async Task GetByCompanyAsync_ProductBelongsToDifferentCompany_ReturnsEmptyList()
        {
            var product = Product.Create(5, 1, 1, "Pizza Grande", null, null, 50m, null, false, null).Value;
            await Context.AddAsync(product);
            await Context.SaveChangesAsync();
            await SeedAsync(CreateConfiguration(productId: product.Id));

            var result = await _repository.GetByCompanyAsync(6);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoConfigurations_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidConfiguration_PersistsToDatabase()
        {
            var configuration = CreateConfiguration(productId: 77);

            await _repository.AddAsync(configuration);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<PizzaConfiguration>()
                .AsNoTracking()
                .Include(x => x.Sizes)
                .FirstOrDefaultAsync(x => x.ProductId == 77);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
            persisted.Sizes.Should().ContainSingle();
        }
    }
}
