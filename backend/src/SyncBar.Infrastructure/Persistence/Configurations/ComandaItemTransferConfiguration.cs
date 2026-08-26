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
    internal sealed class ComandaItemTransferConfiguration : IEntityTypeConfiguration<ComandaItemTransfer>
    {
        public void Configure(EntityTypeBuilder<ComandaItemTransfer> builder)
        {
            builder.ToTable("comandaitemtransfer");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();
            builder.Property(x => x.CustomerOrderId)
                .IsRequired();
            builder.Property(x => x.CustomerOrderItemId)
                .IsRequired();
            builder.Property(x => x.SourceComandaId)
                .IsRequired();
            builder.Property(x => x.TargetComandaId)
                .IsRequired();
            builder.Property(x => x.EmployeeId)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .IsRequired();
            builder.Property(x => x.IsActive)
                .HasDefaultValue(true);
            builder.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}