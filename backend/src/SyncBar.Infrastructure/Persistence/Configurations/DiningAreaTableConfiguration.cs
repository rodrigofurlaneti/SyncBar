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
    internal sealed class DiningAreaTableConfiguration : IEntityTypeConfiguration<DiningAreaTable>
    {
        public void Configure(EntityTypeBuilder<DiningAreaTable> builder)
        {
            builder.ToTable("diningareatable");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.DiningAreaId).IsRequired();
            builder.Property(x => x.DiningTableId).IsRequired();
            builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();
            builder.HasIndex(x => x.DiningTableId)
                   .IsUnique()
                   .HasDatabaseName("UK_DiningAreaTable_Table");
            builder.HasIndex(x => x.DiningAreaId).HasDatabaseName("IX_DiningAreaTable_DiningAreaId");
        }
    }
}
