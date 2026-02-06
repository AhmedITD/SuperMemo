namespace SuperMemo.Application.DTOs.requests.Payments;

public class InitiatePaymentRequest
{
    public required string ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Purpose { get; set; }
    public required string IdempotencyKey { get; set; }
    public string? MerchantId { get; set; }
}
