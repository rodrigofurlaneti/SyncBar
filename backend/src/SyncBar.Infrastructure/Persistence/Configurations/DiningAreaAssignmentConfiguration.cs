using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;
namespace SyncBar.Infrastructure.Persistence.Configurations
{
    internal sealed class DiningAreaAssignmentConfiguration : IEntityTypeConfiguration<DiningAreaAssignment>
    {
        public void Configure(EntityTypeBuilder<DiningAreaAssignment> builder)
        {
            builder.ToTable("diningareaassignment");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.DiningAreaId).IsRequired();
            builder.Property(x => x.EmployeeId).IsRequired();
            builder.Property(x => x.StartAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.EndAt).HasColumnType("datetime(6)");
            builder.Property(x => x.CreatedAt).HasColumnType("datetime(6)").IsRequired();
            builder.Property(x => x.UpdatedAt).HasColumnType("datetime(6)");
            builder.Property(x => x.IsActive).HasColumnType("tinyint(1)").IsRequired();
            builder.HasIndex(x => x.DiningAreaId).HasDatabaseName("IX_DiningAreaAssignment_DiningAreaId");
            builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_DiningAreaAssignment_EmployeeId");
        }
    }
}
