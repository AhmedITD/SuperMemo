using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Cards;

/// <summary>
/// Response when a user creates a new card. Includes the one-time security code (show once, then discard).
/// </summary>
public class CardIssuedResponse
{
    public int Id { get; set; }
    public int AccountId { get; set; }
    public string NumberMasked { get; set; } = null!;
    public CardType Type { get; set; }
    public DateTime ExpiryDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
    public bool IsEmployeeCard { get; set; }
    public DateTime CreatedAt { get; set; }
    /// <summary>One-time display: security code generated for this card. Store securely and do not share.</summary>
    public string SecurityCode { get; set; } = null!;
}
