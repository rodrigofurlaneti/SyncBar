using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ComandaConfiguration : IEntityTypeConfiguration<Comanda>
{
    public void Configure(EntityTypeBuilder<Comanda> builder)
    {
        builder.ToTable("Comanda");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Code).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.ComandaStatusId).HasDatabaseName("IX_Comanda_ComandaStatusId");
        builder.HasIndex(x => new { x.BranchId, x.Code }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Comanda_BranchId_Code");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_Comanda_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ComandaStatus>().WithMany().HasForeignKey(x => x.ComandaStatusId).HasConstraintName("FK_Comanda_ComandaStatus").OnDelete(DeleteBehavior.Restrict);
    }
}
