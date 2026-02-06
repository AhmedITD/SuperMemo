namespace SuperMemo.Application.DTOs.requests.Transactions;

public class CreateTransferRequest
{
    public int FromAccountId { get; set; }
    public required string ToAccountNumber { get; set; }
    public decimal Amount { get; set; }
    public string? Purpose { get; set; }
    /// <summary>Required: send via header <c>Idempotency-Key</c> or in body.</summary>
    public string? IdempotencyKey { get; set; }
}
