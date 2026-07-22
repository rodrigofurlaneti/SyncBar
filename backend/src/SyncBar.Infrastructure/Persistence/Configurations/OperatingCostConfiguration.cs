using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class OperatingCostConfiguration : IEntityTypeConfiguration<OperatingCost>
{
    public void Configure(EntityTypeBuilder<OperatingCost> builder)
    {
        builder.ToTable("OperatingCost");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();

        builder.Property(x => x.Description).HasColumnType("nvarchar(200)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(x => new { x.BranchId, x.ReferenceYear, x.ReferenceMonth })
            .HasDatabaseName("IX_OperatingCost_BranchId_ReferenceYear_ReferenceMonth");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_OperatingCost_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CostType>().WithMany().HasForeignKey(x => x.CostTypeId)
            .HasConstraintName("FK_OperatingCost_CostType").OnDelete(DeleteBehavior.Restrict);
    }
}
