using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class PizzaFlavorRepositoryTests : RepositoryTestBase
    {
        private readonly PizzaFlavorRepository _repository;

        public PizzaFlavorRepositoryTests()
        {
            _repository = new PizzaFlavorRepository(Context);
        }

        private static PizzaFlavor CreateFlavor(long companyId = 1, string name = "Calabresa") =>
            PizzaFlavor.Create(companyId, name, null).Value;

        private async Task<PizzaFlavor> SeedAsync(PizzaFlavor flavor)
        {
            await Context.AddAsync(flavor);
            await Context.SaveChangesAsync();
            Context.Entry(flavor).State = EntityState.Detached;
            return flavor;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedFlavor()
        {
            var flavor = await SeedAsync(CreateFlavor());

            var result = await _repository.GetByIdAsync(flavor.Id);

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
            var flavor = await SeedAsync(CreateFlavor(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(flavor.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedFlavor()
        {
            var flavor = await SeedAsync(CreateFlavor());

            var result = await _repository.GetByIdForUpdateAsync(flavor.Id);

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
        public async Task GetByCompanyAsync_MultipleActiveFlavors_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateFlavor(companyId: 5, name: "F1"));
            var second = await SeedAsync(CreateFlavor(companyId: 5, name: "F2"));
            await SeedAsync(CreateFlavor(companyId: 6, name: "F3"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveFlavor_IsExcluded()
        {
            var flavor = await SeedAsync(CreateFlavor(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(flavor.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoFlavorsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_MatchingIds_ReturnsFlavors()
        {
            var first = await SeedAsync(CreateFlavor(name: "F1"));
            var second = await SeedAsync(CreateFlavor(name: "F2"));
            await SeedAsync(CreateFlavor(name: "F3"));

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
        public async Task AddAsync_ValidFlavor_PersistsToDatabase()
        {
            var flavor = CreateFlavor(companyId: 7, name: "Novo Sabor");

            await _repository.AddAsync(flavor);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<PizzaFlavor>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Novo Sabor");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
