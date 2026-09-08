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
    internal sealed class BranchPaymentMethodSettingConfiguration : IEntityTypeConfiguration<BranchPaymentMethodSetting>
    {
        public void Configure(EntityTypeBuilder<BranchPaymentMethodSetting> builder)
        {
            builder.ToTable("branchpaymentmethodsetting");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();

            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.BranchId);

            builder.Property(x => x.EnablePix)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.EnableBoleto)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.EnableCreditCard)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.EnableDebitCard)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.EnableCashMachine)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .IsRequired();

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime(6)");

            builder.Property(x => x.IsActive)
                .HasColumnType("tinyint(1)")
                .HasDefaultValue(true)
                .IsRequired();

            // Índices
            builder.HasIndex(x => x.CompanyId)
                .HasDatabaseName("IX_BranchPaymentMethodSetting_Company");

            builder.HasIndex(x => x.BranchId)
                .HasDatabaseName("IX_BranchPaymentMethodSetting_Branch");

            // Relacionamentos com deleção restrita
            builder.HasOne<Company>()
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .HasConstraintName("FK_BranchPaymentMethodSetting_Company")
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Branch>()
                .WithMany()
                .HasForeignKey(x => x.BranchId)
                .HasConstraintName("FK_BranchPaymentMethodSetting_Branch")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
