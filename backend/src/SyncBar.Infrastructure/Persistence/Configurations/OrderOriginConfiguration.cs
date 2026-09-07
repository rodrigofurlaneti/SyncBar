using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class OrderOriginConfiguration : IEntityTypeConfiguration<OrderOrigin>
    {
        public void Configure(EntityTypeBuilder<OrderOrigin> builder)
        {
            builder.ToTable("orderorigin");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).HasColumnType("bigint");
            builder.Property(x => x.BranchId).HasColumnType("bigint");
            builder.Property(x => x.Name).HasColumnType("varchar(100)").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired().HasDefaultValue(true);

            builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_OrderOrigin_CompanyId");
            builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_OrderOrigin_BranchId");

            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_OrderOrigin_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_OrderOrigin_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
