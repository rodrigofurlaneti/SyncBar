using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customer");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Phone).HasColumnType("varchar(20)");
        builder.Property(x => x.Cpf).HasColumnType("char(11)");
        builder.Property(x => x.Email).HasColumnType("varchar(150)");
        builder.Property(x => x.LoyaltyPoints).IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_Customer_CompanyId");
        builder.HasIndex(x => new { x.CompanyId, x.Cpf })
            .IsUnique()
            .HasFilter("[Cpf] IS NOT NULL AND [IsActive] = 1")
            .HasDatabaseName("UQ_Customer_Cpf");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).HasConstraintName("FK_Customer_Company").OnDelete(DeleteBehavior.Restrict);
    }
}
