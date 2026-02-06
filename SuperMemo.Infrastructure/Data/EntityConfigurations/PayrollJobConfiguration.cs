using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class PayrollJobConfiguration : IEntityTypeConfiguration<PayrollJob>
{
    public void Configure(EntityTypeBuilder<PayrollJob> builder)
    {
        builder.ToTable("PayrollJobs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.Schedule).HasMaxLength(50);
        builder.Property(x => x.EmployerId).HasMaxLength(100);
        builder.HasOne(x => x.EmployeeUser).WithMany(u => u.PayrollJobsAsEmployee).HasForeignKey(x => x.EmployeeUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
