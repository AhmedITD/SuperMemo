namespace SuperMemo.Domain.Enums;

/// <summary>
/// Status of a payment gateway transaction.
/// </summary>
public enum PaymentStatus
{
    Pending = 0,     // Payment initiated, awaiting gateway response
    Completed = 1,  // Payment successfully completed
    Failed = 2,     // Payment failed
    Cancelled = 3   // Payment was cancelled
}
