using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Company");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.LegalName).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.TradeName).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Cnpj).HasColumnType("char(14)").IsRequired();
        builder.Property(x => x.Email).HasColumnType("varchar(150)");
        builder.Property(x => x.Phone).HasColumnType("varchar(20)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.Cnpj).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Company_Cnpj");
        
    }
}
