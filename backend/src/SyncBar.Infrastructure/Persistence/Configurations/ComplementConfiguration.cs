using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ComplementConfiguration : IEntityTypeConfiguration<Complement>
{
    public void Configure(EntityTypeBuilder<Complement> builder)
    {
        builder.ToTable("Complement");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ExtraPrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        builder.HasIndex(x => x.ComplementGroupId).HasDatabaseName("IX_Complement_ComplementGroupId");
        builder.HasIndex(x => x.ComplementItemId).HasDatabaseName("IX_Complement_ComplementItemId");

        // FK pra ComplementGroup é criada por ComplementGroupConfiguration (HasMany/dono da coleção).
        builder.HasOne<ComplementItem>().WithMany().HasForeignKey(x => x.ComplementItemId)
            .HasConstraintName("FK_Complement_ComplementItem").OnDelete(DeleteBehavior.Restrict);
    }
}
