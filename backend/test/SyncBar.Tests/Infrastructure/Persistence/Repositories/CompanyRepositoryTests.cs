using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Infrastructure.Persistence.Repositories;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    public sealed class CompanyRepositoryTests : RepositoryTestBase
    {
        private readonly CompanyRepository _repository;

        public CompanyRepositoryTests()
        {
            _repository = new CompanyRepository(Context);
        }

        private static Company CreateCompany(string cnpj = "11222333000181") =>
            Company.Create("Razão Social LTDA", "Nome Fantasia", cnpj, "contato@empresa.com", "11999999999").Value;

        private async Task<Company> SeedAsync(Company company)
        {
            await Context.AddAsync(company);
            await Context.SaveChangesAsync();
            Context.Entry(company).State = EntityState.Detached;
            return company;
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUntrackedCompany()
        {
            var company = await SeedAsync(CreateCompany());

            var result = await _repository.GetByIdAsync(company.Id);

            result.Should().NotBeNull();
            result!.Id.Should().Be(company.Id);
            Context.Entry(result).State.Should().Be(EntityState.Detached);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            var result = await _repository.GetByIdAsync(999);

            result.Should().BeNull();
        }

        [Fact]
        public async Task ExistsByCnpjAsync_ExistingCnpj_ReturnsTrue()
        {
            await SeedAsync(CreateCompany(cnpj: "11222333000181"));

            var result = await _repository.ExistsByCnpjAsync("11222333000181");

            result.Should().BeTrue();
        }

        [Fact]
        public async Task ExistsByCnpjAsync_NonExistingCnpj_ReturnsFalse()
        {
            var result = await _repository.ExistsByCnpjAsync("00000000000000");

            result.Should().BeFalse();
        }

        [Fact]
        public async Task AddAsync_ValidCompany_PersistsToDatabase()
        {
            var company = CreateCompany(cnpj: "22333444000195");

            await _repository.AddAsync(company);
            await Context.SaveChangesAsync();

            var persisted = await Context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Cnpj == "22333444000195");

            persisted.Should().NotBeNull();
            persisted!.Id.Should().BeGreaterThan(0);
        }
    }
}
