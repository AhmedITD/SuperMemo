using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Interfaces;

namespace SuperMemo.Domain.Entities;

public class User : BaseEntity, IAuditable, ISoftDeletable
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public required string PasswordHash { get; set; }
    public UserRole Role { get; set; }

    public KycStatus KycStatus { get; set; } = KycStatus.Pending;
    public KybStatus KybStatus { get; set; } = KybStatus.Pending;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.PendingApproval;

    public int? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Account? Account { get; set; }
    public ICollection<IcDocument> IcDocuments { get; set; } = new List<IcDocument>();
    public ICollection<PassportDocument> PassportDocuments { get; set; } = new List<PassportDocument>();
    public ICollection<LivingIdentityDocument> LivingIdentityDocuments { get; set; } = new List<LivingIdentityDocument>();
    public ICollection<PayrollJob> PayrollJobsAsEmployee { get; set; } = new List<PayrollJob>();
}
