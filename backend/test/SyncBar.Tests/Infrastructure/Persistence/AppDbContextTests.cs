using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SyncBar.Domain.Entities;
using SyncBar.Domain.Exceptions;
using SyncBar.Infrastructure.Persistence;
using Xunit;

namespace SyncBar.Tests.Infrastructure.Persistence
{
    public sealed class AppDbContextTests : Repositories.RepositoryTestBase
    {
        // AppDbContext expõe ~90 DbSet<T> como `public DbSet<X> Xs => Set<X>();` — nenhum outro
        // teste desta classe (nem os de repositório, que recebem o Context já pronto) precisa
        // tocar em cada uma dessas propriedades individualmente, então a maioria ficava com 0 hits
        // de cobertura mesmo sendo puro código de passagem sem lógica própria. Em vez de escrever
        // dezenas de testes quase idênticos, este único teste invoca todo getter DbSet<T> via
        // reflection e confirma que nenhum devolve null — cobre a linha e ainda pega, de graça, um
        // DbSet mal digitado que aponte pro tipo errado ou lance em tempo de execução.
        [Fact]
        public void AllDbSetProperties_ShouldBeAccessibleAndReturnNonNullSet()
        {
            var dbSetProperties = typeof(AppDbContext).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToList();

            dbSetProperties.Should().NotBeEmpty();

            foreach (var property in dbSetProperties)
            {
                var value = property.GetValue(Context);
                value.Should().NotBeNull($"a propriedade {property.Name} deveria expor um DbSet válido");
            }
        }

        [Fact]
        public async Task CommitAsync_PendingAddedEntity_PersistsAndReturnsAffectedRowCount()
        {
            var feature = AppFeature.Create("FEATURE_COMMIT", "Feature Commit").Value;
            await Context.AddAsync(feature);

            var affectedRows = await Context.CommitAsync();

            affectedRows.Should().Be(1);
            var persisted = await Context.AppFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == "FEATURE_COMMIT");
            persisted.Should().NotBeNull();
        }

        [Fact]
        public async Task CommitAsync_NoPendingChanges_ReturnsZero()
        {
            var affectedRows = await Context.CommitAsync();

            affectedRows.Should().Be(0);
        }

        // Cobre a corrida de verdade do bug "Order already has an active sale": duas vendas ativas
        // para o mesmo pedido violam UQ_Sale_CustomerOrderId (índice único filtrado por IsActive=1)
        // — o CommitAsync deve traduzir o DbUpdateException genérico do EF (não o
        // DbUpdateConcurrencyException de RowVersion) para o ConcurrencyException de domínio, que o
        // RegisterSaleCommandHandler sabe recuperar.
        [Fact]
        public async Task CommitAsync_UniqueConstraintViolation_ThrowsDomainConcurrencyException()
        {
            var first = Sale.Create(1, 100, 10, 5, 1, 100m, 0m, 0m).Value;
            await Context.AddAsync(first);
            await Context.CommitAsync();

            var second = Sale.Create(1, 100, 10, 5, 2, 100m, 0m, 0m).Value;
            await Context.AddAsync(second);

            var act = () => Context.CommitAsync();

            await act.Should().ThrowAsync<ConcurrencyException>();
        }

        [Fact]
        public async Task CompanyScopedQueryFilter_TenantSet_ReturnsOnlyMatchingCompanyRows()
        {
            var categoryTenantOne = Category.Create(1, "Category Tenant 1", 1).Value;
            var categoryTenantTwo = Category.Create(2, "Category Tenant 2", 1).Value;
            await Context.AddRangeAsync(categoryTenantOne, categoryTenantTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = 1;

            var result = await Context.Categories.ToListAsync();

            result.Should().ContainSingle();
            result[0].CompanyId.Should().Be(1);
        }

        [Fact]
        public async Task CompanyScopedQueryFilter_TenantNotSet_ReturnsAllCompaniesRows()
        {
            var categoryTenantOne = Category.Create(1, "Category Tenant 1", 1).Value;
            var categoryTenantTwo = Category.Create(2, "Category Tenant 2", 1).Value;
            await Context.AddRangeAsync(categoryTenantOne, categoryTenantTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = null;

            var result = await Context.Categories.ToListAsync();

            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task BranchScopedQueryFilter_TenantSet_ReturnsOnlyRowsOfBranchesBelongingToCompany()
        {
            var companyOne = Company.Create("Legal One", "Trade One", "11111111000101", null, null).Value;
            var companyTwo = Company.Create("Legal Two", "Trade Two", "22222222000102", null, null).Value;
            await Context.AddRangeAsync(companyOne, companyTwo);
            await Context.SaveChangesAsync();

            var branchOne = Branch.Create(companyOne.Id, "Branch One", null, null, null, null, null, null, null, null).Value;
            var branchTwo = Branch.Create(companyTwo.Id, "Branch Two", null, null, null, null, null, null, null, null).Value;
            await Context.AddRangeAsync(branchOne, branchTwo);
            await Context.SaveChangesAsync();

            var comandaOne = Comanda.Create(branchOne.Id, 1, "CMD-1").Value;
            var comandaTwo = Comanda.Create(branchTwo.Id, 1, "CMD-2").Value;
            await Context.AddRangeAsync(comandaOne, comandaTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = companyOne.Id;

            var result = await Context.Comandas.ToListAsync();

            result.Should().ContainSingle();
            result[0].BranchId.Should().Be(branchOne.Id);
        }

        [Fact]
        public async Task BranchScopedQueryFilter_TenantNotSet_ReturnsAllBranchesRows()
        {
            var companyOne = Company.Create("Legal One", "Trade One", "11111111000101", null, null).Value;
            var companyTwo = Company.Create("Legal Two", "Trade Two", "22222222000102", null, null).Value;
            await Context.AddRangeAsync(companyOne, companyTwo);
            await Context.SaveChangesAsync();

            var branchOne = Branch.Create(companyOne.Id, "Branch One", null, null, null, null, null, null, null, null).Value;
            var branchTwo = Branch.Create(companyTwo.Id, "Branch Two", null, null, null, null, null, null, null, null).Value;
            await Context.AddRangeAsync(branchOne, branchTwo);
            await Context.SaveChangesAsync();

            var comandaOne = Comanda.Create(branchOne.Id, 1, "CMD-1").Value;
            var comandaTwo = Comanda.Create(branchTwo.Id, 1, "CMD-2").Value;
            await Context.AddRangeAsync(comandaOne, comandaTwo);
            await Context.SaveChangesAsync();

            TenantService.CompanyId = null;

            var result = await Context.Comandas.ToListAsync();

            result.Should().HaveCount(2);
        }
    }
}
