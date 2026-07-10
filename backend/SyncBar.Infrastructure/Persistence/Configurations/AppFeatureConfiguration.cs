using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AppFeatureConfiguration : IEntityTypeConfiguration<AppFeature>
{
    public void Configure(EntityTypeBuilder<AppFeature> builder)
    {
        builder.ToTable("AppFeature");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.Code).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Name).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_AppFeature_Code");
    }
}
