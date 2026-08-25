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
    internal sealed class WaiterMessageConfiguration : IEntityTypeConfiguration<WaiterMessage>
    {
        public void Configure(EntityTypeBuilder<WaiterMessage> builder)
        {
            builder.ToTable("waitermessage");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.BranchId).IsRequired();
            builder.Property(x => x.SenderEmployeeId).IsRequired();
            builder.Property(x => x.Message).HasColumnType("varchar(500)").IsRequired();
            builder.Property(x => x.IsRead).HasColumnType("tinyint(1)").IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();
            builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_WaiterMessage_BranchId");
        }
    }
}
