using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class ComplementGroupRepositoryTests : RepositoryTestBase
    {
        private readonly ComplementGroupRepository _repository;

        public ComplementGroupRepositoryTests()
        {
            _repository = new ComplementGroupRepository(Context);
        }

        private static ComplementGroup CreateGroup(long companyId = 1, string name = "Adicionais") =>
            ComplementGroup.Create(companyId, name, 1, 0, 1).Value;

        private async Task<ComplementGroup> SeedAsync(ComplementGroup group)
        {
            await Context.AddAsync(group);
            await Context.SaveChangesAsync();
            Context.Entry(group).State = EntityState.Detached;
            return group;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedGroupWithComplements()
        {
            var group = CreateGroup();
            group.AddComplement(1, 5m);
            await SeedAsync(group);

            var result = await _repository.GetByIdAsync(group.Id);

            result.Should().NotBeNull();
            result!.Complements.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Detached);
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
            var group = await SeedAsync(CreateGroup(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(group.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_ExistingId_ReturnsTrackedGroupWithComplements()
        {
            var group = CreateGroup();
            group.AddComplement(1, 5m);
            await SeedAsync(group);

            var result = await _repository.GetByIdForUpdateAsync(group.Id);

            result.Should().NotBeNull();
            result!.Complements.Should().ContainSingle();
            Context.Entry(result).State.Should().Be(EntityState.Unchanged);
        }

        [Fact]
        public async Task GetByIdForUpdateAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdForUpdateAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_MultipleActiveGroups_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateGroup(companyId: 5, name: "G1"));
            var second = await SeedAsync(CreateGroup(companyId: 5, name: "G2"));
            await SeedAsync(CreateGroup(companyId: 6, name: "G3"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveGroup_IsExcluded()
        {
            var group = await SeedAsync(CreateGroup(companyId: 5));
            var tracked = await _repository.GetByIdForUpdateAsync(group.Id);
            tracked!.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoGroupsForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByIdsAsync_MatchingIds_ReturnsGroups()
        {
            var first = await SeedAsync(CreateGroup(name: "G1"));
            var second = await SeedAsync(CreateGroup(name: "G2"));
            await SeedAsync(CreateGroup(name: "G3"));

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
        public async Task AddAsync_ValidGroup_PersistsToDatabase()
        {
            var group = CreateGroup(companyId: 7, name: "Novo Grupo");

            await _repository.AddAsync(group);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<ComplementGroup>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Novo Grupo");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
