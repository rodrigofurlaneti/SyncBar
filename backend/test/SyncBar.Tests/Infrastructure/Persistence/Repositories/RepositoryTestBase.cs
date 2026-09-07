using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using SyncBar.Application.Abstractions.Tenancy;
using SyncBar.Infrastructure.Persistence;

namespace SyncBar.Tests.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// Base compartilhada pelos testes de repositório: cada teste ganha uma conexão SQLite em
    /// memória isolada (uma instância nova de classe de teste por [Fact]/[Theory] no xUnit já garante
    /// isolamento, sem precisar de limpeza manual entre testes) com o schema criado via
    /// EnsureCreated() a partir do próprio modelo do AppDbContext — incluindo os HasQueryFilter de
    /// tenant, que são o motivo de usar SQLite real em vez do provider InMemory do EF.
    /// </summary>
    public abstract class RepositoryTestBase : IDisposable
    {
        protected readonly SqliteConnection Connection;
        protected readonly AppDbContext Context;
        protected readonly FakeCurrentTenantService TenantService;

        protected RepositoryTestBase()
        {
            // "Foreign Keys=False": o Microsoft.Data.Sqlite ativa PRAGMA foreign_keys=1 por padrão,
            // o que exigiria semear toda a árvore de dependências (Company, Branch, etc.) em cada um
            // dos 76 testes de repositório só pra satisfazer FKs irrelevantes ao comportamento sendo
            // testado. Testes unitários de repositório isolam UMA entidade por vez — não são teste de
            // integridade referencial do schema inteiro.
            Connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=False");
            Connection.Open();

            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(Connection)
                .ReplaceService<IModelCustomizer, SqliteCompatibleModelCustomizer>()
                .Options;

            TenantService = new FakeCurrentTenantService();
            Context = new AppDbContext(options, TenantService);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
            GC.SuppressFinalize(this);
        }
    }

    public sealed class FakeCurrentTenantService : ICurrentTenantService
    {
        public long? CompanyId { get; set; }
    }

    /// <summary>
    /// O modelo de produção usa `HasDefaultValueSql("CURRENT_TIMESTAMP(6)")` (sintaxe do MySQL) em
    /// algumas colunas de auditoria — o SQLite não reconhece `CURRENT_TIMESTAMP(6)` como expressão de
    /// DEFAULT válida ("near '(': syntax error" ao rodar EnsureCreated). A aplicação nunca depende
    /// desse default no nível do banco (cada entidade já define CreatedAt no próprio construtor de
    /// domínio antes do INSERT), então é seguro remover essas expressões só para os testes.
    ///
    /// Também neutraliza `.IsRowVersion()` (ex.: ProductStock.RowVersion): esse mapeamento espera que
    /// o banco gere o valor no INSERT/UPDATE (como o `rowversion` do SQL Server) — o SQLite não tem
    /// esse mecanismo, e como a propriedade some do INSERT (fica a cargo do banco), o resultado é
    /// "NOT NULL constraint failed". Trocando pra `ValueGenerated.Never` o EF volta a enviar o valor
    /// que já vem do C# (`= []` por padrão) — perde-se só a checagem de concorrência otimista via
    /// banco, irrelevante para testes unitários de repositório isolados.
    ///
    /// Também converte colunas `TimeSpan`/`TimeSpan?` (ex.: IfoodOpeningHours.Start) pra ticks
    /// (long): o provider SQLite não suporta `ORDER BY` em `TimeSpan` ("SQLite does not support
    /// expressions of type 'TimeSpan' in ORDER BY clauses") — MySQL ordena `time`/`timespan`
    /// nativamente sem esse problema, então essa conversão é só uma adaptação de teste.
    /// </summary>
    public sealed class SqliteCompatibleModelCustomizer(ModelCustomizerDependencies dependencies)
        : ModelCustomizer(dependencies)
    {
        public override void Customize(ModelBuilder modelBuilder, DbContext context)
        {
            base.Customize(modelBuilder, context);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetDefaultValueSql() is not null)
                    {
                        property.SetDefaultValueSql(null);
                    }

                    if (property.IsConcurrencyToken && property.ValueGenerated == ValueGenerated.OnAddOrUpdate)
                    {
                        property.ValueGenerated = ValueGenerated.Never;
                    }

                    if (property.ClrType == typeof(TimeSpan))
                    {
                        property.SetValueConverter(new TimeSpanToTicksConverter());
                    }
                    else if (property.ClrType == typeof(TimeSpan?))
                    {
                        property.SetValueConverter(new ValueConverter<TimeSpan?, long?>(
                            v => v.HasValue ? v.Value.Ticks : null,
                            v => v.HasValue ? TimeSpan.FromTicks(v.Value) : null));
                    }
                }
            }
        }
    }
}
