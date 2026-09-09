using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemBoostConfiguration : IEntityTypeConfiguration<OrderItemBoost>
{
    public void Configure(EntityTypeBuilder<OrderItemBoost> builder)
    {
        builder.ToTable("orderitemboost");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.UnitPriceCharged).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne<ProductBoost>().WithMany().HasForeignKey(x => x.ProductBoostId).OnDelete(DeleteBehavior.Restrict);
    }
}

