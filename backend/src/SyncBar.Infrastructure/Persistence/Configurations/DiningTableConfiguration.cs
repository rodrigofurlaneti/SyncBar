using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class DiningTableConfiguration : IEntityTypeConfiguration<DiningTable>
{
    public void Configure(EntityTypeBuilder<DiningTable> builder)
    {
        builder.ToTable("DiningTable");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.QrToken);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.TableStatusId).HasDatabaseName("IX_DiningTable_TableStatusId");

        // Achado de revisão: os dois índices abaixo usavam `.HasFilter("[IsActive] = 1")` /
        // `.HasFilter("[QrToken] IS NOT NULL")` — sintaxe de índice único FILTRADO do SQL Server
        // (colchetes), mas este projeto roda em MySQL (confirmado em `DependencyInjection.cs`,
        // `UseMySql(...)`), que não tem suporte confiável a índice único parcial/filtrado. Como o
        // schema real é criado pelos scripts SQL em `sql/` (não por migrations do EF Core), esse
        // `HasFilter` nunca chegava a gerar DDL nenhum — era só metadado inerte, mas induzia a
        // achar que a exclusão condicional (permitir reaproveitar número de mesa após soft
        // delete) estava garantida pelo banco, quando não está.
        //
        // `QrToken`: MySQL permite múltiplos valores NULL num índice único (diferente do SQL
        // Server, que só aceita um NULL sem índice filtrado) — um índice único simples já resolve
        // sem precisar de filtro nenhum.
        builder.HasIndex(x => x.QrToken).IsUnique().HasDatabaseName("UQ_DiningTable_QrToken");

        // `(BranchId, Number)`: já que MySQL não tem o mesmo truque de NULL do QrToken (IsActive
        // nunca é nulo), um índice único simples aqui bloquearia reaproveitar o número de uma
        // mesa depois de desativada (soft delete) — diferente da intenção original do filtro.
        // Hoje isso não é um bug ativo: não existe nenhum CreateDiningTableCommandHandler no
        // projeto (mesas são inseridas só via script/seed SQL, não pela API), então não há
        // nenhuma rotina de aplicação que dependa dessa exclusividade condicional ainda. Se um
        // fluxo de criar/desativar mesa pela API for implementado no futuro, seguir o mesmo
        // padrão de get-or-create já usado no resto do projeto para unicidade condicional em
        // MySQL (ver `IFoodProductMapping`/`ProductComplementGroup`) em vez de tentar recriar o
        // índice filtrado.
        builder.HasIndex(x => new { x.BranchId, x.Number }).HasDatabaseName("IX_DiningTable_BranchId_Number");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_DiningTable_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TableStatus>().WithMany().HasForeignKey(x => x.TableStatusId).HasConstraintName("FK_DiningTable_TableStatus").OnDelete(DeleteBehavior.Restrict);
    }
}
