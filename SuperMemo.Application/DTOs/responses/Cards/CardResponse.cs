using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Cards;

public class CardResponse
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string NumberMasked { get; set; } = null!; // e.g. ****1234
    public CardType Type { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsEmployeeCard { get; set; }
    public DateTime CreatedAt { get; set; }
}
