using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ProductOptionalExtraConfiguration : IEntityTypeConfiguration<ProductOptionalExtra>
{
    public void Configure(EntityTypeBuilder<ProductOptionalExtra> builder)
    {
        builder.ToTable("productoptionalextra");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.OptionalExtraName).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasIndex(x => new { x.ProductId, x.DisplayOrder });
        builder.HasOne<Product>().WithMany(x => x.OptionalExtras).HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

