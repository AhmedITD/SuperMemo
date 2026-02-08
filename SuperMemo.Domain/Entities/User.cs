using System.Text.Json.Serialization;
using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Interfaces;

namespace SuperMemo.Domain.Entities;

public class User : BaseEntity, IAuditable, ISoftDeletable
{
    public required string FullName { get; set; }
    public required string Phone { get; set; }

    /// <summary>Hashed in backend only; never serialized to API responses.</summary>
    [JsonIgnore]
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    /// <summary>Optional profile/avatar image URL (stored in external storage).</summary>
    public string? ImageUrl { get; set; }

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
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
