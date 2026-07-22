using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CashSessionStatusConfiguration : IEntityTypeConfiguration<CashSessionStatus>
{
    public void Configure(EntityTypeBuilder<CashSessionStatus> builder)
    {
        builder.ToTable("CashSessionStatus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Name).HasColumnType("nvarchar(50)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
    }
}
