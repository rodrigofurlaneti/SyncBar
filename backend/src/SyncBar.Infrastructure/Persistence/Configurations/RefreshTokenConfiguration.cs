using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshToken");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Token).HasColumnType("varchar(500)").IsRequired();
        builder.Property(x => x.ExpiresAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.RevokedAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        
        builder.HasIndex(x => x.AppUserId).HasDatabaseName("IX_RefreshToken_AppUserId");
        
        builder.HasOne<AppUser>().WithMany().HasForeignKey(x => x.AppUserId).HasConstraintName("FK_RefreshToken_AppUser").OnDelete(DeleteBehavior.Cascade);
    }
}
