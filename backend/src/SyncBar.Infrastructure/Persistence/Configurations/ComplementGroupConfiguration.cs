using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ComplementGroupConfiguration : IEntityTypeConfiguration<ComplementGroup>
{
    public void Configure(EntityTypeBuilder<ComplementGroup> builder)
    {
        builder.ToTable("ComplementGroup");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        // Constante de código (sem tabela) — ver ComplementGroupTypeIds. CHECK 1-4 no
        // sql/BarRestaurante_Complementos.sql, mesmo padrão de OrderTypeId.
        builder.Property(x => x.ComplementGroupTypeId).HasColumnType("tinyint").IsRequired();
        builder.Property(x => x.MinSelection).IsRequired();
        builder.Property(x => x.MaxSelection).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_ComplementGroup_CompanyId");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_ComplementGroup_Company").OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Complements).WithOne().HasForeignKey(c => c.ComplementGroupId)
            .HasConstraintName("FK_Complement_ComplementGroup").OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Complements).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
