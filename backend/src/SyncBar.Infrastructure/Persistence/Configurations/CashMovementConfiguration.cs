using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CashMovementConfiguration : IEntityTypeConfiguration<CashMovement>
{
    public void Configure(EntityTypeBuilder<CashMovement> builder)
    {
        // Nome da tabela ajustado para minúsculo conforme o padrão do MySQL
        builder.ToTable("cashmovement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();

        // Ajustado de nvarchar para varchar conforme o dump do MySQL
        builder.Property(x => x.Description).HasColumnType("varchar(300)");

        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Índices de relacionamento existentes
        builder.HasIndex(x => x.CashSessionId).HasDatabaseName("IX_CashMovement_CashSessionId");
        builder.HasIndex(x => x.CashMovementTypeId).HasDatabaseName("IX_CashMovement_CashMovementTypeId");
        builder.HasIndex(x => x.SaleId).HasDatabaseName("IX_CashMovement_SaleId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_CashMovement_EmployeeId");

        // Novo índice adicionado para otimização de relatórios por data de movimentação
        builder.HasIndex(x => x.CreatedAt).HasDatabaseName("IX_CashMovement_CreatedAt");

        builder.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).HasConstraintName("FK_CashMovement_CashSession").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CashMovementType>().WithMany().HasForeignKey(x => x.CashMovementTypeId).HasConstraintName("FK_CashMovement_CashMovementType").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Sale>().WithMany().HasForeignKey(x => x.SaleId).HasConstraintName("FK_CashMovement_Sale").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_CashMovement_Employee").OnDelete(DeleteBehavior.Restrict);
    }
}