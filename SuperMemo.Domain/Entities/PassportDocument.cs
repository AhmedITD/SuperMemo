using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class PassportDocument : BaseEntity
{
    public int UserId { get; set; }
    public required string PassportNumber { get; set; }
    public required string FullName { get; set; }
    public string? ShortName { get; set; }
    public required string Nationality { get; set; }
    public DateTime BirthDate { get; set; }
    public required string MotherFullName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public KycDocumentStatus Status { get; set; } = KycDocumentStatus.Pending;

    public User User { get; set; } = null!;
}
