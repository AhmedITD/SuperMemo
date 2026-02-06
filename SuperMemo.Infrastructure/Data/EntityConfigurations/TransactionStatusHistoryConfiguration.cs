using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class TransactionStatusHistoryConfiguration : IEntityTypeConfiguration<TransactionStatusHistory>
{
    public void Configure(EntityTypeBuilder<TransactionStatusHistory> builder)
    {
        builder.ToTable("TransactionStatusHistory");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.HasOne(x => x.Transaction).WithMany().HasForeignKey(x => x.TransactionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.TransactionId);
        builder.HasIndex(x => x.ChangedAt);
    }
}
