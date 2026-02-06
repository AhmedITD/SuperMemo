namespace SuperMemo.Domain.Enums;

/// <summary>
/// Classification of transaction failure reasons.
/// Temporary failures can be retried; permanent failures should not be retried.
/// </summary>
public enum FailureReason
{
    // Temporary failures (retry allowed)
    NetworkTimeout = 0,
    ServiceUnavailable = 1,
    ConcurrencyConflict = 2,
    
    // Permanent failures (no retry)
    InsufficientFunds = 10,
    InvalidDestination = 11,
    RiskBlocked = 12,
    AccountFrozen = 13,
    AccountClosed = 14,
    TransactionExpired = 15
}
