using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Transactions;

public class TransactionResponse
{
    public int Id { get; set; }
    public int FromAccountId { get; set; }
    public string ToAccountNumber { get; set; } = null!;
    public decimal Amount { get; set; }
    /// <summary>"DEBIT" (money out) or "CREDIT" (money in) from the perspective of the account viewing the transaction.</summary>
    public string TransactionType { get; set; } = null!; // "DEBIT" | "CREDIT"
    public TransactionStatus Status { get; set; }
    public string? Purpose { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Enhanced fields
    public FailureReason? FailureReason { get; set; }
    public int RetryCount { get; set; }
    public int? RiskScore { get; set; }
    public RiskLevel? RiskLevel { get; set; }
    public DateTime? StatusChangedAt { get; set; }
    
    // Retry information
    public bool? RetryRecommended { get; set; }
    public int? RetryAfterSeconds { get; set; }
    public int? MaxRetries { get; set; } = 3;
}
