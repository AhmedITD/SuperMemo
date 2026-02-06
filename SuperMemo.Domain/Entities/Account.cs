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

    public User User { get; set; } = null!;
    public ICollection<Card> Cards { get; set; } = new List<Card>();
    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
}
