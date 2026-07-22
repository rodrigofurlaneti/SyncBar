using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Product");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(500)");
        builder.Property(x => x.Barcode).HasColumnType("varchar(50)");
        builder.Property(x => x.SalePrice).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.ImageUrl).HasColumnType("nvarchar(300)");
        builder.Property(x => x.CostPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_Product_CompanyId");
        builder.HasIndex(x => x.CategoryId).HasDatabaseName("IX_Product_CategoryId");
        builder.HasIndex(x => x.UnitOfMeasureId).HasDatabaseName("IX_Product_UnitOfMeasureId");
        
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).HasConstraintName("FK_Product_Company").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Category>().WithMany().HasForeignKey(x => x.CategoryId).HasConstraintName("FK_Product_Category").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => x.UnitOfMeasureId).HasConstraintName("FK_Product_UnitOfMeasure").OnDelete(DeleteBehavior.Restrict);
    }
}
