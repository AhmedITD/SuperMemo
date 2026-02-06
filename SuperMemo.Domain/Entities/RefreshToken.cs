namespace SuperMemo.Domain.Entities;

public class RefreshToken
{
    public Guid Id { get; set; }
    public required string TokenHash { get; set; }
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? DeviceClientId { get; set; }
    public string? IpAddress { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;
}
