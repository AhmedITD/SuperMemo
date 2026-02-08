using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class Transaction : BaseEntity
{
    public int FromAccountId { get; set; }
    public required string ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; } = TransactionType.Debit;
    public TransactionStatus Status { get; set; } = TransactionStatus.Created;
    public string? Purpose { get; set; }
    public string? IdempotencyKey { get; set; }
    
    /// <summary>Category of transaction: Transfer, TopUp, Withdraw, or Interest.</summary>
    public TransactionCategory Category { get; set; } = TransactionCategory.Transfer;
    
    /// <summary>Linked payment record (for TOP_UP transactions).</summary>
    public int? PaymentId { get; set; }
    
    // Enhanced fields for advanced features
    /// <summary>Reason for transaction failure (if status is Failed).</summary>
    public FailureReason? FailureReason { get; set; }
    /// <summary>Number of retry attempts made for this transaction.</summary>
    public int RetryCount { get; set; } = 0;
    /// <summary>Fraud risk score (0-100).</summary>
    public int? RiskScore { get; set; }
    /// <summary>Fraud risk level (LOW, MEDIUM, HIGH).</summary>
    public RiskLevel? RiskLevel { get; set; }
    /// <summary>Timestamp when status was last changed.</summary>
    public DateTime? StatusChangedAt { get; set; }

    public Account FromAccount { get; set; } = null!;
    public Payment? Payment { get; set; }
}
