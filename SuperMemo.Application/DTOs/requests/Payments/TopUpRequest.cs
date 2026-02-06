namespace SuperMemo.Application.DTOs.requests.Payments;

public class TopUpRequest
{
    public int AccountId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string RequestId { get; set; }
}
