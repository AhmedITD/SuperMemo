using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("Transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.ToAccountNumber).IsRequired().HasMaxLength(34);
        builder.Property(x => x.IdempotencyKey).HasMaxLength(64);
        builder.HasOne(x => x.FromAccount).WithMany(a => a.OutgoingTransactions).HasForeignKey(x => x.FromAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FromAccountId, x.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL AND \"IdempotencyKey\" != ''");
    }
}
