using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IFoodMerchantMappingConfiguration : IEntityTypeConfiguration<IFoodMerchantMapping>
{
    public void Configure(EntityTypeBuilder<IFoodMerchantMapping> builder)
    {
        builder.ToTable("IFoodMerchantMapping");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.MerchantId).HasMaxLength(100);
        builder.Property(x => x.MerchantUuid).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado — mesmo motivo de sempre (MySQL sem índice parcial). "1
        // mapeamento ativo por filial" é garantido pelo handler (upsert por BranchId).
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_IFoodMerchantMapping_BranchId");

        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId)
            .HasConstraintName("FK_IFoodMerchantMapping_Branch").OnDelete(DeleteBehavior.Restrict);
    }
}
