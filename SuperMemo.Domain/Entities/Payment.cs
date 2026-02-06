using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

/// <summary>
/// Represents a payment gateway transaction (e.g., QiCard top-up).
/// </summary>
public class Payment : BaseEntity
{
    public int UserId { get; set; }
    public int AccountId { get; set; }
    
    /// <summary>Payment gateway name (e.g., "QiCard").</summary>
    public required string PaymentGateway { get; set; }
    
    /// <summary>Payment ID returned by the gateway.</summary>
    public string? GatewayPaymentId { get; set; }
    
    /// <summary>Unique request ID for idempotency.</summary>
    public required string RequestId { get; set; }
    
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    
    /// <summary>Redirect URL from gateway for user to complete payment.</summary>
    public string? PaymentUrl { get; set; }
    
    /// <summary>Linked transaction record (created when payment succeeds).</summary>
    public int? TransactionId { get; set; }
    
    /// <summary>Raw JSON response from gateway.</summary>
    public string? GatewayResponse { get; set; }
    
    /// <summary>Whether webhook was received for this payment.</summary>
    public bool WebhookReceived { get; set; } = false;
    
    /// <summary>Webhook payload data (JSON).</summary>
    public string? WebhookData { get; set; }

    // Navigation properties
    public User User { get; set; } = null!;
    public Account Account { get; set; } = null!;
    public Transaction? Transaction { get; set; }
    public ICollection<PaymentWebhookLog> WebhookLogs { get; set; } = new List<PaymentWebhookLog>();
}
