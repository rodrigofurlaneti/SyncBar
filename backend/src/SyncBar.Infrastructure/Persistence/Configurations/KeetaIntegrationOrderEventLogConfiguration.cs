using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class KeetaIntegrationOrderEventLogConfiguration : IEntityTypeConfiguration<KeetaIntegrationOrderEventLog>
    {
        public void Configure(EntityTypeBuilder<KeetaIntegrationOrderEventLog> builder)
        {
            builder.ToTable("keetaintegrationordereventlog");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .HasMaxLength(36)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId).IsRequired();

            builder.Property(x => x.EventId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.OrderId)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.EventType)
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(x => x.RawPayload)
                .HasColumnType("json");

            builder.Property(x => x.ProcessedSuccessfully)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasColumnType("text");

            builder.Property(x => x.EventCreatedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.ReceivedAtUtc)
                .HasColumnType("datetime(6)")
                .IsRequired();

            // Índices
            builder.HasIndex(x => x.EventId)
                .IsUnique()
                .HasDatabaseName("uid_event_id");

            builder.HasIndex(x => x.OrderId)
                .HasDatabaseName("idx_event_order_id");

            // Relacionamentos
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_KeetaEventLog_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_KeetaEventLog_Branch")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<KeetaIntegrationOrder>()
                .WithMany()
                .HasPrincipalKey(x => x.KeetaOrderId)
                .HasForeignKey(x => x.OrderId)
                .HasConstraintName("FK_KeetaEventLog_Order")
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
