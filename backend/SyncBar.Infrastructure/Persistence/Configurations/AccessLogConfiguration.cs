using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AccessLogConfiguration : IEntityTypeConfiguration<AccessLog>
{
    public void Configure(EntityTypeBuilder<AccessLog> builder)
    {
        builder.ToTable("AccessLog");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.UserName).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.EventType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.IpAddress).HasColumnType("varchar(45)");
        builder.Property(x => x.UserAgent).HasColumnType("nvarchar(300)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.AppUserId).HasDatabaseName("IX_AccessLog_AppUserId");
        
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AppUserId).HasConstraintName("FK_AccessLog_AppUser").OnDelete(DeleteBehavior.Restrict);
    }
}
