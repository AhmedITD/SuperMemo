using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class LivingIdentityDocument : BaseEntity
{
    public int UserId { get; set; }
    public required string SerialNumber { get; set; }
    public required string FullFamilyName { get; set; }
    public required string LivingLocation { get; set; }
    public required string FormNumber { get; set; }
    public string? ImageUrl { get; set; }
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;

    public User User { get; set; } = null!;
}
