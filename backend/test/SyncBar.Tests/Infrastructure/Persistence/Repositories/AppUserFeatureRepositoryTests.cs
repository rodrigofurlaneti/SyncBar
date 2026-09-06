using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class AppUserFeatureRepositoryTests : RepositoryTestBase
    {
        private readonly AppUserFeatureRepository _repository;

        public AppUserFeatureRepositoryTests()
        {
            _repository = new AppUserFeatureRepository(Context);
        }

        private static AppUserFeature CreateLink(long appUserId = 1, long appFeatureId = 1) =>
            AppUserFeature.Create(appUserId, appFeatureId).Value;

        private async Task<AppUserFeature> SeedAsync(AppUserFeature link)
        {
            await Context.AddAsync(link);
            await Context.SaveChangesAsync();
            Context.Entry(link).State = EntityState.Detached;
            return link;
        }

        [Fact]
        public async Task GetByUserAsync_MultipleActiveLinks_ReturnsAllForUser()
        {
            var first = await SeedAsync(CreateLink(appUserId: 5, appFeatureId: 1));
            var second = await SeedAsync(CreateLink(appUserId: 5, appFeatureId: 2));
            await SeedAsync(CreateLink(appUserId: 6, appFeatureId: 3));

            var result = await _repository.GetByUserAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByUserAsync_DeactivatedLink_IsExcluded()
        {
            var link = await SeedAsync(CreateLink(appUserId: 5));
            var tracked = (await _repository.GetByUserForUpdateAsync(5)).Single();
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByUserAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUserAsync_NoLinksForUser_ReturnsEmptyList()
        {
            var result = await _repository.GetByUserAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByUserForUpdateAsync_IncludesInactiveLinks_ReturnsTrackedEntities()
        {
            var link = await SeedAsync(CreateLink(appUserId: 5));
            var tracked = await _repository.GetByUserForUpdateAsync(5);
            tracked.Single().Deactivate();
            await Context.SaveChangesAsync();

            var result = await Context.Set<AppUserFeature>().AsNoTracking().Where(x => x.Id == link.Id).ToListAsync();

            result.Single().IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByUserForUpdateAsync_NoLinks_ReturnsEmptyList()
        {
            var result = await _repository.GetByUserForUpdateAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidLink_PersistsToDatabase()
        {
            var link = CreateLink(appUserId: 10, appFeatureId: 20);

            await _repository.AddAsync(link);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<AppUserFeature>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.AppUserId == 10 && x.AppFeatureId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
