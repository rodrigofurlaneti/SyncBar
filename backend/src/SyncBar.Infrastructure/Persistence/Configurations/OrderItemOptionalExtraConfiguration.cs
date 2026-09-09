using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OrderItemOptionalExtraConfiguration : IEntityTypeConfiguration<OrderItemOptionalExtra>
{
    public void Configure(EntityTypeBuilder<OrderItemOptionalExtra> builder)
    {
        builder.ToTable("orderitemoptionalextra");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Name).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.HasOne<ProductOptionalExtra>().WithMany().HasForeignKey(x => x.ProductOptionalExtraId).OnDelete(DeleteBehavior.Restrict);
    }
}

