using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ComplementItemConfiguration : IEntityTypeConfiguration<ComplementItem>
{
    public void Configure(EntityTypeBuilder<ComplementItem> builder)
    {
        builder.ToTable("ComplementItem");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_ComplementItem_CompanyId");
        // Fase 18 (combos) — só preenchido quando o item aponta pra um Product real do cardápio.
        builder.HasIndex(x => x.LinkedProductId).HasDatabaseName("IX_ComplementItem_LinkedProductId");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_ComplementItem_Company").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Product>().WithMany().HasForeignKey(x => x.LinkedProductId)
            .HasConstraintName("FK_ComplementItem_LinkedProduct").OnDelete(DeleteBehavior.Restrict);
    }
}
