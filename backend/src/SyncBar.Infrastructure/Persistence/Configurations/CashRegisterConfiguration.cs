using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CashRegisterConfiguration : IEntityTypeConfiguration<CashRegister>
{
    public void Configure(EntityTypeBuilder<CashRegister> builder)
    {
        builder.ToTable("CashRegister");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        
        builder.Property(x => x.Name).HasColumnType("nvarchar(100)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
        
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_CashRegister_BranchId");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_CashRegister_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
