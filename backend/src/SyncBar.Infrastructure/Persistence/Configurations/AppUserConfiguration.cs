using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AppUser");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.UserName).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.Email).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.PasswordSalt).HasColumnType("varchar(200)");
        builder.Property(x => x.LockoutEndAt).HasColumnType("datetime2");
        builder.Property(x => x.LastLoginAt).HasColumnType("datetime2");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_AppUser_CompanyId");
        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_AppUser_EmployeeId");
        builder.HasIndex(x => x.UserName).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_AppUser_UserName");
        builder.HasIndex(x => x.Email).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_AppUser_Email");
        
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).HasConstraintName("FK_AppUser_Company").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).HasConstraintName("FK_AppUser_Employee").OnDelete(DeleteBehavior.Restrict);
    }
}
