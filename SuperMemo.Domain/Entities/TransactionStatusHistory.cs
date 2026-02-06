using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

/// <summary>
/// Optional audit trail table for tracking transaction status changes.
/// </summary>
public class TransactionStatusHistory : BaseEntity
{
    public int TransactionId { get; set; }
    public TransactionStatus OldStatus { get; set; }
    public TransactionStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    /// <summary>User ID who changed the status (null for system changes).</summary>
    public int? ChangedBy { get; set; }
    public string? Reason { get; set; }

    public Transaction Transaction { get; set; } = null!;
}
