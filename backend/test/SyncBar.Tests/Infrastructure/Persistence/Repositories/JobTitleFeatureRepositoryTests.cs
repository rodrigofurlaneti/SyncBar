using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class JobTitleFeatureRepositoryTests : RepositoryTestBase
    {
        private readonly JobTitleFeatureRepository _repository;

        public JobTitleFeatureRepositoryTests()
        {
            _repository = new JobTitleFeatureRepository(Context);
        }

        private static JobTitleFeature CreateLink(long jobTitleId = 1, long appFeatureId = 1) =>
            JobTitleFeature.Create(jobTitleId, appFeatureId).Value;

        private async Task<JobTitleFeature> SeedAsync(JobTitleFeature link)
        {
            await Context.AddAsync(link);
            await Context.SaveChangesAsync();
            Context.Entry(link).State = EntityState.Detached;
            return link;
        }

        [Fact]
        public async Task GetByJobTitleAsync_MultipleActiveLinks_ReturnsAllForJobTitle()
        {
            var first = await SeedAsync(CreateLink(jobTitleId: 5, appFeatureId: 1));
            var second = await SeedAsync(CreateLink(jobTitleId: 5, appFeatureId: 2));
            await SeedAsync(CreateLink(jobTitleId: 6, appFeatureId: 3));

            var result = await _repository.GetByJobTitleAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByJobTitleAsync_DeactivatedLink_IsExcluded()
        {
            var link = await SeedAsync(CreateLink(jobTitleId: 5));
            var tracked = (await _repository.GetByJobTitleForUpdateAsync(5)).Single();
            tracked.Deactivate();
            await Context.SaveChangesAsync();

            var result = await _repository.GetByJobTitleAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByJobTitleAsync_NoLinksForJobTitle_ReturnsEmptyList()
        {
            var result = await _repository.GetByJobTitleAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByJobTitleForUpdateAsync_IncludesInactiveLinks()
        {
            var link = await SeedAsync(CreateLink(jobTitleId: 5));
            var tracked = await _repository.GetByJobTitleForUpdateAsync(5);
            tracked.Single().Deactivate();
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<JobTitleFeature>().AsNoTracking().FirstAsync(x => x.Id == link.Id);

            persisted.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task GetByJobTitleForUpdateAsync_NoLinks_ReturnsEmptyList()
        {
            var result = await _repository.GetByJobTitleForUpdateAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidLink_PersistsToDatabase()
        {
            var link = CreateLink(jobTitleId: 10, appFeatureId: 20);

            await _repository.AddAsync(link);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<JobTitleFeature>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobTitleId == 10 && x.AppFeatureId == 20);

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
