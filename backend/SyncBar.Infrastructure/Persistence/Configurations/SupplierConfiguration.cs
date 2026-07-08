using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Supplier");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.LegalName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.TradeName).HasColumnType("nvarchar(150)");
        builder.Property(x => x.Cnpj).HasColumnType("char(14)");
        builder.Property(x => x.Email).HasColumnType("varchar(150)");
        builder.Property(x => x.Phone).HasColumnType("varchar(20)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_Supplier_CompanyId");
        
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).HasConstraintName("FK_Supplier_Company").OnDelete(DeleteBehavior.Restrict);
    }
}
