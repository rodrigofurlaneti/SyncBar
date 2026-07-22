using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class JobTitleFeatureConfiguration : IEntityTypeConfiguration<JobTitleFeature>
{
    public void Configure(EntityTypeBuilder<JobTitleFeature> builder)
    {
        builder.ToTable("JobTitleFeature");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.JobTitleId, x.AppFeatureId })
            .IsUnique().HasFilter("[IsActive] = 1")
            .HasDatabaseName("UQ_JobTitleFeature_JobTitleId_AppFeatureId");

        builder.HasOne<JobTitle>().WithMany().HasForeignKey(x => x.JobTitleId)
            .HasConstraintName("FK_JobTitleFeature_JobTitle").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<AppFeature>().WithMany().HasForeignKey(x => x.AppFeatureId)
            .HasConstraintName("FK_JobTitleFeature_AppFeature").OnDelete(DeleteBehavior.Restrict);
    }
}
