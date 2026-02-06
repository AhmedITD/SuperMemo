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
    /// <summary>Client-provided key to prevent duplicate transfers. Unique per from-account (or scope).</summary>
    public string? IdempotencyKey { get; set; }

    public Account FromAccount { get; set; } = null!;
}
