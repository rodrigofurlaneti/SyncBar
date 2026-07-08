using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRole");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.AppUserId).HasDatabaseName("IX_UserRole_AppUserId");
        builder.HasIndex(x => x.RoleId).HasDatabaseName("IX_UserRole_RoleId");
        builder.HasIndex(x => new { x.AppUserId, x.RoleId }).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_UserRole_AppUserId_RoleId");
        
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AppUserId).HasConstraintName("FK_UserRole_AppUser").OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Role>().WithMany().HasForeignKey(x => x.RoleId).HasConstraintName("FK_UserRole_Role").OnDelete(DeleteBehavior.Restrict);
    }
}
