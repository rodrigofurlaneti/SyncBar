using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PizzaFlavorConfiguration : IEntityTypeConfiguration<PizzaFlavor>
{
    public void Configure(EntityTypeBuilder<PizzaFlavor> builder)
    {
        builder.ToTable("PizzaFlavor");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(500)");
        builder.Property(x => x.ImageUrl).HasColumnType("nvarchar(300)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_PizzaFlavor_CompanyId");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_PizzaFlavor_Company").OnDelete(DeleteBehavior.Restrict);
    }
}
