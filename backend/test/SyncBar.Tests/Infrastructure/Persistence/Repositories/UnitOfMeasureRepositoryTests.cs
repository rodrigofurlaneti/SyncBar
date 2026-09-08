using FluentAssertions;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class UnitOfMeasureRepositoryTests : RepositoryTestBase
    {
        private readonly UnitOfMeasureRepository _repository;

        public UnitOfMeasureRepositoryTests()
        {
            _repository = new UnitOfMeasureRepository(Context);
        }

        [Fact]
        public async Task GetFirstActiveAsync_ActiveUnitsExist_ReturnsLowestId()
        {
            var first = UnitOfMeasure.Create("Unidade", "un").Value;
            var second = UnitOfMeasure.Create("Quilograma", "kg").Value;
            await Context.AddRangeAsync(first, second);
            await Context.SaveChangesAsync();

            var result = await _repository.GetFirstActiveAsync();

            result.Should().NotBeNull();
            result!.Id.Should().Be(first.Id);
        }

        [Fact]
        public async Task GetFirstActiveAsync_OnlyInactiveUnitsExist_ReturnsNull()
        {
            var unit = UnitOfMeasure.Create("Unidade", "un").Value;
            unit.Deactivate();
            await Context.AddAsync(unit);
            await Context.SaveChangesAsync();

            var result = await _repository.GetFirstActiveAsync();

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetFirstActiveAsync_NoUnitsExist_ReturnsNull()
        {
            var result = await _repository.GetFirstActiveAsync();

            result.Should().BeNull();
        }
    }
}
