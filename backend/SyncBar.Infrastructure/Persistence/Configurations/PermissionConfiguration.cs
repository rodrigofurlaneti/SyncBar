using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permission");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Code).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.ModuleName).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.Code).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Permission_Code");
        
    }
}
