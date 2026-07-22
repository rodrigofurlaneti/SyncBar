using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermission");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.RoleId).HasDatabaseName("IX_RolePermission_RoleId");
        builder.HasIndex(x => x.PermissionId).HasDatabaseName("IX_RolePermission_PermissionId");
        builder.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_RolePermission_RoleId_PermissionId");
        
        builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).HasConstraintName("FK_RolePermission_Role").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).HasConstraintName("FK_RolePermission_Permission").OnDelete(DeleteBehavior.Restrict);
    }
}
