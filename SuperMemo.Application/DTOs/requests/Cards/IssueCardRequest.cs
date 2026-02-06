using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Cards;

public class IssueCardRequest
{
    public int AccountId { get; set; }
    public required string Number { get; set; }
    public CardType Type { get; set; }
    public DateTime ExpiryDate { get; set; }
    public required string SecurityCode { get; set; }
    public bool IsEmployeeCard { get; set; }
}
