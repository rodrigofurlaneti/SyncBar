using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class IfoodPizzaMappingRepositoryTests : RepositoryTestBase
    {
        private readonly IfoodPizzaMappingRepository _repository;

        public IfoodPizzaMappingRepositoryTests()
        {
            _repository = new IfoodPizzaMappingRepository(Context);
        }

        private static IfoodPizzaMapping CreateMapping(long pizzaConfigurationId = 1, long branchId = 1, string ifoodPizzaId = "pizza-1") =>
            IfoodPizzaMapping.Create(pizzaConfigurationId, branchId, ifoodPizzaId).Value;

        private async Task<IfoodPizzaMapping> SeedWithElementAsync(IfoodPizzaMapping mapping)
        {
            await Context.AddAsync(mapping);
            await Context.SaveChangesAsync();
            mapping.SetElement(IfoodPizzaElementKind.Size, localId: 1, "element-1");
            await Context.SaveChangesAsync();
            Context.Entry(mapping).State = EntityState.Detached;
            return mapping;
        }

        [Fact]
        public async Task GetByPizzaConfigurationAndBranchAsync_ExistingActiveMapping_ReturnsUntrackedMappingWithElements()
        {
            var mapping = await SeedWithElementAsync(CreateMapping(pizzaConfigurationId: 5, branchId: 10));

            var result = await _repository.GetByPizzaConfigurationAndBranchAsync(5, 10);

            result.Should().NotBeNull();
            result!.Elements.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByPizzaConfigurationAndBranchAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByPizzaConfigurationAndBranchAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByPizzaConfigurationAndBranchForUpdateAsync_ExistingActiveMapping_ReturnsTrackedMappingWithElements()
        {
            var mapping = await SeedWithElementAsync(CreateMapping(pizzaConfigurationId: 5, branchId: 10));

            var result = await _repository.GetByPizzaConfigurationAndBranchForUpdateAsync(5, 10);

            result.Should().NotBeNull();
            result!.Elements.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByPizzaConfigurationAndBranchForUpdateAsync_NonExisting_ReturnsNull()
        {
            var result = await _repository.GetByPizzaConfigurationAndBranchForUpdateAsync(999, 999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByBranchAsync_MultipleActiveMappings_ReturnsAllForBranch()
        {
            var first = await SeedWithElementAsync(CreateMapping(pizzaConfigurationId: 1, branchId: 5, ifoodPizzaId: "pizza-1"));
            var second = await SeedWithElementAsync(CreateMapping(pizzaConfigurationId: 2, branchId: 5, ifoodPizzaId: "pizza-2"));
            await SeedWithElementAsync(CreateMapping(pizzaConfigurationId: 3, branchId: 6, ifoodPizzaId: "pizza-3"));

            var result = await _repository.GetByBranchAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByBranchAsync_NoMappingsForBranch_ReturnsEmptyList()
        {
            var result = await _repository.GetByBranchAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidMapping_PersistsToDatabase()
        {
            var mapping = CreateMapping(ifoodPizzaId: "pizza-added");

            await _repository.AddAsync(mapping);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<IfoodPizzaMapping>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IfoodPizzaId == "pizza-added");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
