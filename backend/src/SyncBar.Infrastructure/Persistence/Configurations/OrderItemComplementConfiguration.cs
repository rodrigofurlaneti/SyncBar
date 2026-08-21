using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemComplementConfiguration : IEntityTypeConfiguration<OrderItemComplement>
{
    public void Configure(EntityTypeBuilder<OrderItemComplement> builder)
    {
        builder.ToTable("OrderItemComplement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.UnitPriceCharged).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.ComplementId).HasDatabaseName("IX_OrderItemComplement_ComplementId");

        // FK pra OrderItem é criada por OrderItemConfiguration (HasMany/dono da coleção).
        builder.HasOne<Complement>().WithMany().HasForeignKey(x => x.ComplementId)
            .HasConstraintName("FK_OrderItemComplement_Complement").OnDelete(DeleteBehavior.Restrict);
    }
}
