using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Admin;

public class UserApprovalListItemResponse
{
    public int Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? Phone { get; set; }
    public KycStatus KycStatus { get; set; }
    public KybStatus KybStatus { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public int? AccountId { get; set; }
    public string? AccountNumber { get; set; }
    public AccountStatus? AccountStatus { get; set; }
}

