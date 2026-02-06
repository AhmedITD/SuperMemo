using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("Accounts");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.AccountNumber).IsUnique();
        builder.Property(x => x.Balance).HasPrecision(18, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.AccountNumber).IsRequired().HasMaxLength(34);
        builder.HasOne(x => x.User).WithOne(u => u.Account).HasForeignKey<Account>(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        
        // Account type fields
        builder.Property(x => x.AccountType).IsRequired().HasDefaultValue(Domain.Enums.AccountType.Regular);
        builder.Property(x => x.DailySpendingLimit).HasPrecision(18, 4);
        builder.Property(x => x.DailySpentAmount).HasPrecision(18, 4).HasDefaultValue(0);
        
        // Indexes for performance
        builder.HasIndex(x => x.AccountType);
        builder.HasIndex(x => x.LastInterestCalculationDate);
        builder.HasIndex(x => x.LastDailyLimitResetDate);
    }
}
