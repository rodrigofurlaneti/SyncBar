using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AppUserFeatureConfiguration : IEntityTypeConfiguration<AppUserFeature>
{
    public void Configure(EntityTypeBuilder<AppUserFeature> builder)
    {
        builder.ToTable("AppUserFeature");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.AppUserId, x.AppFeatureId })
            .IsUnique().HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_AppUserFeature_AppUserId_AppFeatureId");

        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AppUserId)
            .HasConstraintName("FK_AppUserFeature_AppUser").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppFeature>().WithMany().HasForeignKey(x => x.AppFeatureId)
            .HasConstraintName("FK_AppUserFeature_AppFeature").OnDelete(DeleteBehavior.Restrict);
    }
}
