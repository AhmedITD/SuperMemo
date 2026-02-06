using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Payments;

public class PaymentResponse
{
    public int Id { get; set; }
    public string PaymentGateway { get; set; } = null!;
    public string? GatewayPaymentId { get; set; }
    public string RequestId { get; set; } = null!;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public PaymentStatus Status { get; set; }
    public string? PaymentUrl { get; set; }
    public int? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
