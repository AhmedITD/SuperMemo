using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class Card : BaseEntity
{
    public int AccountId { get; set; }
    public required string Number { get; set; }
    public CardType Type { get; set; }
    public DateTime ExpiryDate { get; set; }
    public required string ScHashed { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsExpired { get; set; }
    public bool IsEmployeeCard { get; set; }

    public Account Account { get; set; } = null!;
}
