using SuperMemo.Domain.Entities.Common;

namespace SuperMemo.Domain.Entities;

public class PhoneVerificationCode : BaseEntity
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsUsed { get; set; }
    public string? IpAddress { get; set; }

    public bool IsExpired() => DateTime.UtcNow > ExpiresAt;
    public bool IsValid() => !IsUsed && !IsExpired();
}
