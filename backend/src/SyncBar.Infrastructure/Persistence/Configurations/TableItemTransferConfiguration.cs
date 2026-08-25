using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class TableItemTransferConfiguration : IEntityTypeConfiguration<TableItemTransfer>
    {
        public void Configure(EntityTypeBuilder<TableItemTransfer> builder)
        {
            builder.ToTable("tableitemtransfer");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.CustomerOrderId).IsRequired();
            builder.Property(x => x.CustomerOrderItemId).IsRequired();
            builder.Property(x => x.SourceDiningTableId).IsRequired();
            builder.Property(x => x.TargetDiningTableId).IsRequired();
            builder.Property(x => x.EmployeeId).IsRequired();
            builder.Property(x => x.CreatedAt).IsRequired();
            builder.Property(x => x.IsActive).IsRequired();
            builder.HasOne<CustomerOrder>()
                .WithMany()
                .HasForeignKey(x => x.CustomerOrderId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<DiningTable>()
                .WithMany()
                .HasForeignKey(x => x.SourceDiningTableId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<DiningTable>()
                .WithMany()
                .HasForeignKey(x => x.TargetDiningTableId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<Employee>()
                .WithMany()
                .HasForeignKey(x => x.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
