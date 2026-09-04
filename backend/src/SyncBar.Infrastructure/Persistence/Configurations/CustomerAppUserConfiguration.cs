using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CustomerAppUserConfiguration : IEntityTypeConfiguration<CustomerAppUser>
{
    public void Configure(EntityTypeBuilder<CustomerAppUser> builder)
    {
        builder.ToTable("customerappuser");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.BranchId);
        builder.Property(x => x.CustomerId);
        builder.Property(x => x.UserName).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.Email).HasColumnType("varchar(150)").IsRequired();
        builder.Property(x => x.PasswordHash).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.FailedAccessCount).HasDefaultValue(0).IsRequired();
        builder.Property(x => x.LockoutEndAt).HasColumnType("datetime(6)");
        builder.Property(x => x.LastLoginAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").HasDefaultValueSql("CURRENT_TIMESTAMP(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_CustomerAppUser_Company")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Branch>()
            .WithMany()
            .HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_CustomerAppUser_Branch")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(x => x.CustomerId)
            .HasConstraintName("FK_CustomerAppUser_Customer")
            .OnDelete(DeleteBehavior.Restrict);
    }
}