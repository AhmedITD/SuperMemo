using SuperMemo.Domain.Entities.Common;

namespace SuperMemo.Domain.Entities;

/// <summary>
/// Optional table for managing merchant accounts that can receive NFC/QR payments.
/// </summary>
public class MerchantAccount : BaseEntity
{
    public int AccountId { get; set; }
    public required string MerchantId { get; set; }
    public required string MerchantName { get; set; }
    public string? QrCodeData { get; set; }
    public string? NfcUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public Account Account { get; set; } = null!;
}
