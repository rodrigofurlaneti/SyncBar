using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SyncBar.Domain.Entities;

namespace SyncBar.Infrastructure.Persistence.Configurations;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employee");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).UseIdentityColumn();
        
        builder.Property(x => x.Name).HasColumnType("nvarchar(150)").IsRequired();
        builder.Property(x => x.Cpf).HasColumnType("char(11)").IsRequired();
        builder.Property(x => x.Email).HasColumnType("varchar(150)");
        builder.Property(x => x.Phone).HasColumnType("varchar(20)");
        builder.Property(x => x.HiredAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.DismissedAt).HasColumnType("datetime2");
        builder.Property(x => x.Salary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("datetime2");
        
        builder.HasIndex(x => x.BranchId).HasDatabaseName("IX_Employee_BranchId");
        builder.HasIndex(x => x.JobTitleId).HasDatabaseName("IX_Employee_JobTitleId");
        builder.HasIndex(x => x.Cpf).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UQ_Employee_Cpf");
        
        builder.HasOne<Branch>().WithMany().HasForeignKey(x => x.BranchId).HasConstraintName("FK_Employee_Branch").OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<JobTitle>().WithMany().HasForeignKey(x => x.JobTitleId).HasConstraintName("FK_Employee_JobTitle").OnDelete(DeleteBehavior.Restrict);
    }
}
