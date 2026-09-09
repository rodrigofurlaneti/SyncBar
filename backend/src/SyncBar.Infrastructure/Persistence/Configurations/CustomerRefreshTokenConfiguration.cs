using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CustomerRefreshTokenConfiguration : IEntityTypeConfiguration<CustomerRefreshToken>
{
    public void Configure(EntityTypeBuilder<CustomerRefreshToken> builder)
    {
        builder.ToTable("CustomerRefreshToken");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Token).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        
        builder.HasIndex(x => x.CustomerAppUserId).HasDatabaseName("IX_CustomerRefreshToken_CustomerAppUserId");
        
        builder.HasOne<CustomerAppUser>().WithMany().HasForeignKey(x => x.CustomerAppUserId).HasConstraintName("FK_CustomerRefreshToken_CustomerAppUser").OnDelete(DeleteBehavior.Cascade);
    }
}
