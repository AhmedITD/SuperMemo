using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Admin;

public class UserStatusResponse
{
    public int UserId { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public KycStatus KycStatus { get; set; }
    public KybStatus KybStatus { get; set; }
    public AccountStatus? AccountStatus { get; set; }
    public string StatusDescription { get; set; } = null!;
}
