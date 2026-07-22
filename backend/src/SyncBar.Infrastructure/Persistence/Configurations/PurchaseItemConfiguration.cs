using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PurchaseItemConfiguration : IEntityTypeConfiguration<PurchaseItem>
{
    public void Configure(EntityTypeBuilder<PurchaseItem> builder)
    {
        builder.ToTable("PurchaseItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Quantity).HasColumnType("decimal(18,3)").IsRequired();
        builder.Property(x => x.UnitCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.TotalCost).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.PurchaseId).HasDatabaseName("IX_PurchaseItem_PurchaseId");
        builder.HasIndex(x => x.ProductId).HasDatabaseName("IX_PurchaseItem_ProductId");
        
        // Relação com Purchase é declarada em PurchaseConfiguration via HasMany(x => x.Items) —
        // não duplicar aqui (evita EF criar duas FKs para a mesma coluna PurchaseId).
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).HasConstraintName("FK_PurchaseItem_Product").OnDelete(DeleteBehavior.Restrict);
    }
}
