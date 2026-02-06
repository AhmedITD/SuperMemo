using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class Account : BaseEntity
{
    public int UserId { get; set; }
    public decimal Balance { get; set; }
    public required string Currency { get; set; }
    public AccountStatus Status { get; set; } = AccountStatus.PendingApproval;
    /// <summary>Unique account number for transfers (e.g. IBAN-like or generated number).</summary>
    public required string AccountNumber { get; set; }
    
    /// <summary>Account type: Regular or Savings. Immutable after creation.</summary>
    public AccountType AccountType { get; set; } = AccountType.Regular;
    
    /// <summary>Daily spending limit for Savings accounts (calculated as 5% of balance).</summary>
    public decimal? DailySpendingLimit { get; set; }
    
    /// <summary>Amount spent today (resets daily for Savings accounts).</summary>
    public decimal DailySpentAmount { get; set; } = 0;
    
    /// <summary>Last date interest was calculated (for Savings accounts).</summary>
    public DateTime? LastInterestCalculationDate { get; set; }
    
    /// <summary>Last date daily limit was reset (for Savings accounts).</summary>
    public DateTime? LastDailyLimitResetDate { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
}
