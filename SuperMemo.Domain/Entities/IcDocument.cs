using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class IcDocument : BaseEntity
{
    public int UserId { get; set; }
    public required string IdentityCardNumber { get; set; }
    public required string FullName { get; set; }
    public required string MotherFullName { get; set; }
    public DateTime BirthDate { get; set; }
    public required string BirthLocation { get; set; }
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;

    public User User { get; set; } = null!;
}
