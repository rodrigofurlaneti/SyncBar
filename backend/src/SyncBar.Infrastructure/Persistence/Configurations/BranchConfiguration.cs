using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branch");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Cnpj).HasColumnType("char(14)");
        builder.Property(x => x.Phone).HasColumnType("varchar(20)");
        builder.Property(x => x.AddressStreet).HasColumnType("nvarchar(200)");
        builder.Property(x => x.AddressNumber).HasColumnType("varchar(20)");
        builder.Property(x => x.AddressDistrict).HasColumnType("nvarchar(100)");
        builder.Property(x => x.AddressCity).HasColumnType("nvarchar(100)");
        builder.Property(x => x.AddressState).HasColumnType("char(2)");
        builder.Property(x => x.AddressZipCode).HasColumnType("char(8)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_Branch_CompanyId");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).HasConstraintName("FK_Branch_Company").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.SelfServiceEmployeeId).HasConstraintName("FK_Branch_SelfServiceEmployee").OnDelete(DeleteBehavior.Restrict);
    }
}
