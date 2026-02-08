using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Cards;

public class CreateMyCardRequest
{
    public CardType Type { get; set; }
}
