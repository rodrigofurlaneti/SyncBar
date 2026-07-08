using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseConfiguration : IEntityTypeConfiguration<Purchase>
{
    public void Configure(EntityTypeBuilder<Purchase> builder)
    {
        builder.ToTable("Purchase");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.DocumentNumber).HasColumnType("varchar(50)");
        builder.Property(x => x.PurchasedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.TotalAmount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(500)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_Purchase_BranchId");
        builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_Purchase_SupplierId");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_Purchase_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).HasConstraintName("FK_Purchase_Supplier").OnDelete(DeleteBehavior.Restrict);
    }
}
