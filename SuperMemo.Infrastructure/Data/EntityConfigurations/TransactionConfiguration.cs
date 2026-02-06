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
        builder.Property(x => x.FailureReason);
        builder.Property(x => x.RetryCount).HasDefaultValue(0);
        builder.Property(x => x.RiskScore);
        builder.Property(x => x.Category).HasDefaultValue(SuperMemo.Domain.Enums.TransactionCategory.Transfer);
        
        builder.HasOne(x => x.FromAccount).WithMany(a => a.OutgoingTransactions).HasForeignKey(x => x.FromAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Payment)
            .WithOne(p => p.Transaction)
            .HasForeignKey<Transaction>(x => x.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
            
        builder.HasIndex(x => new { x.FromAccountId, x.IdempotencyKey }).IsUnique().HasFilter("\"IdempotencyKey\" IS NOT NULL AND \"IdempotencyKey\" != ''");
        
        // Performance indexes for new fields
        builder.HasIndex(x => new { x.Status, x.StatusChangedAt });
        builder.HasIndex(x => new { x.RiskLevel, x.Status });
        builder.HasIndex(x => new { x.FromAccountId, x.CreatedAt });
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasIndex(x => x.Category);
    }
}
