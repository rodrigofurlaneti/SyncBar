using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class IfoodIntegrationSettingConfiguration : IEntityTypeConfiguration<IfoodIntegrationSetting>
{
    public void Configure(EntityTypeBuilder<IfoodIntegrationSetting> builder)
    {
        builder.ToTable("IfoodIntegrationSetting");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.ClientId).HasMaxLength(200);
        builder.Property(x => x.ClientSecretEncrypted).HasColumnType("varchar(1000)");
        builder.Property(x => x.IfoodCustomerId).HasMaxLength(100);
        builder.Property(x => x.Enabled).HasColumnType("bit").IsRequired();
        builder.Property(x => x.LastConnectionTestSucceeded).HasColumnType("bit");
        builder.Property(x => x.LastConnectionTestAt).HasColumnType("datetime(6)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");

        // Sem índice único filtrado — MySQL não tem índice parcial nativo. "1 config ativa
        // por empresa" é garantida pelo handler (upsert por CompanyId), igual ServiceFeeSetting.
        builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_IfoodIntegrationSetting_CompanyId");

        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId)
            .HasConstraintName("FK_IfoodIntegrationSetting_Company").OnDelete(DeleteBehavior.Restrict);
    }
}
