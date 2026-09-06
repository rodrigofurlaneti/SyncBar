using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationSettingConfiguration : IEntityTypeConfiguration<KeetaIntegrationSetting>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationSetting> builder)
        {
            builder.ToTable("keetaintegrationsetting");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();

            // Opcionais: KeetaIntegrationSetting.Create(companyId, branchId) cria o registro antes de
            // qualquer credencial existir — SaveCredentials() é chamado depois, separadamente. Marcar
            // como IsRequired() aqui contradiz a nulabilidade (string?) da entidade e quebra esse fluxo.
            builder.Property(x => x.ClientId)
                .HasMaxLength(255);

            builder.Property(x => x.ClientSecret)
                .HasMaxLength(255);

            builder.Property(x => x.AppId)
                .HasMaxLength(255);

            builder.Property(x => x.BaseUrl)
                .HasMaxLength(255)
                .HasDefaultValue("https://open.mykeeta.com/api/open/opendelivery");

            builder.Property(x => x.CurrentAccessToken)
                .HasColumnType("text");

            builder.Property(x => x.TokenExpiresAtUtc)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.UpdatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            // Índices
            builder.HasIndex(x => x.CompanyId).HasDatabaseName("IX_KeetaSetting_Company");
            builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_KeetaSetting_Branch");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaSetting_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaSetting_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
