using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationAuthorizationSessionConfiguration : IEntityTypeConfiguration<KeetaIntegrationAuthorizationSession>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationAuthorizationSession> builder)
        {
            builder.ToTable("keetaintegrationauthorizationsession");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();

            builder.Property(x => x.AuthId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.State)
                .HasMaxLength(255);

            builder.Property(x => x.KeetaMerchantId);

            builder.Property(x => x.AuthorizationCode)
                .HasMaxLength(255);

            builder.Property(x => x.OperationType)
                .IsRequired();

            builder.Property(x => x.IsProcessed)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            // Índices
            builder.HasIndex(x => x.AuthId)
                .HasDatabaseName("idx_auth_id");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaSession_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaSession_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
