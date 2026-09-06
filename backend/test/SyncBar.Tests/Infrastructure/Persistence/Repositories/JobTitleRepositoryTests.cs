using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class JobTitleRepositoryTests : RepositoryTestBase
    {
        private readonly JobTitleRepository _repository;

        public JobTitleRepositoryTests()
        {
            _repository = new JobTitleRepository(Context);
        }

        private static JobTitle CreateJobTitle(long companyId = 1, string name = "Garçom") =>
            JobTitle.Create(companyId, name).Value;

        private async Task<JobTitle> SeedAsync(JobTitle jobTitle)
        {
            await Context.AddAsync(jobTitle);
            await Context.SaveChangesAsync();
            Context.Entry(jobTitle).State = EntityState.Detached;
            return jobTitle;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedJobTitle()
        {
            var jobTitle = await SeedAsync(CreateJobTitle());

            var result = await _repository.GetByIdAsync(jobTitle.Id);

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
            var jobTitle = await SeedAsync(CreateJobTitle(companyId: 5));
            TenantService.CompanyId = 6;

            var result = await _repository.GetByIdAsync(jobTitle.Id);

            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByCompanyAsync_MultipleActiveJobTitles_ReturnsAllForCompany()
        {
            var first = await SeedAsync(CreateJobTitle(companyId: 5, name: "Garçom"));
            var second = await SeedAsync(CreateJobTitle(companyId: 5, name: "Cozinheiro"));
            await SeedAsync(CreateJobTitle(companyId: 6, name: "Gerente"));

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().HaveCount(2);
            result.Select(x => x.Id).Should().Contain([first.Id, second.Id]);
        }

        [Fact]
        public async Task GetByCompanyAsync_InactiveJobTitle_IsExcluded()
        {
            var jobTitle = await SeedAsync(CreateJobTitle(companyId: 5));
            jobTitle.Deactivate();
            Context.Update(jobTitle);
            await Context.SaveChangesAsync();

            var result = await _repository.GetByCompanyAsync(5);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task GetByCompanyAsync_NoJobTitlesForCompany_ReturnsEmptyList()
        {
            var result = await _repository.GetByCompanyAsync(999);

            result.Should().BeEmpty();
        }

        [Fact]
        public async Task AddAsync_ValidJobTitle_PersistsToDatabase()
        {
            var jobTitle = CreateJobTitle(companyId: 7, name: "Caixa");

            await _repository.AddAsync(jobTitle);
            await Context.SaveChangesAsync();

            var persisted = await Context.Set<JobTitle>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == "Caixa");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
