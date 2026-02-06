using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Fraud;

/// <summary>
/// Service for classifying transaction failures as temporary or permanent.
/// </summary>
public interface IFailureClassificationService
{
    /// <summary>
    /// Classifies a failure and determines if it's temporary (retryable) or permanent.
    /// </summary>
    FailureClassification ClassifyFailure(Exception error, TransactionContext? context = null);
}

/// <summary>
/// Result of failure classification.
/// </summary>
public class FailureClassification
{
    public bool IsTemporary { get; set; }
    public FailureReason FailureReason { get; set; }
    public string? Message { get; set; }
    public int? RetryAfterSeconds { get; set; }
}

/// <summary>
/// Context information for failure classification.
/// </summary>
public class TransactionContext
{
    public int? AccountId { get; set; }
    public decimal? Amount { get; set; }
    public string? ToAccountNumber { get; set; }
}
