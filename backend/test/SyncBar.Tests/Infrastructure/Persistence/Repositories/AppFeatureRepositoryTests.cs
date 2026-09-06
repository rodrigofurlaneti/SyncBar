using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AppFeatureRepositoryTests : RepositoryTestBase
    {
        private readonly AppFeatureRepository _repository;

        public AppFeatureRepositoryTests()
        {
            _repository = new AppFeatureRepository(Context);
        }

        [Fact]
        public async Task GetAllAsync_MultipleActiveFeatures_ReturnsAll()
        {
            var first = AppFeature.Create("FEATURE_A", "Feature A").Value;
            var second = AppFeature.Create("FEATURE_B", "Feature B").Value;
            await Context.AddRangeAsync(first, second);
            await Context.SaveChangesAsync();

            var result = await _repository.GetAllAsync();

            result.Select(x => x.Code).Should().Contain(["FEATURE_A", "FEATURE_B"]);
        }

        [Fact]
        public async Task GetAllAsync_NoFeatures_ReturnsEmptyList()
        {
            var result = await _repository.GetAllAsync();

            result.Should().BeEmpty();
        }
    }
}
