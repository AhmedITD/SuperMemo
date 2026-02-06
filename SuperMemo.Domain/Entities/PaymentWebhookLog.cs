using SuperMemo.Domain.Entities.Common;

namespace SuperMemo.Domain.Entities;

/// <summary>
/// Audit log for payment webhook processing.
/// </summary>
public class PaymentWebhookLog : BaseEntity
{
    public int PaymentId { get; set; }
    
    /// <summary>Raw webhook payload (JSON).</summary>
    public required string WebhookPayload { get; set; }
    
    /// <summary>Signature from webhook header.</summary>
    public string? Signature { get; set; }
    
    /// <summary>Whether signature was verified.</summary>
    public bool SignatureValid { get; set; }
    
    /// <summary>Whether webhook was processed successfully.</summary>
    public bool Processed { get; set; } = false;
    
    /// <summary>When webhook was processed.</summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>Error message if processing failed.</summary>
    public string? ErrorMessage { get; set; }

    // Navigation property
    public Payment Payment { get; set; } = null!;
}
