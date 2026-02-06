using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Admin;

public class UserListItemResponse
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public UserRole Role { get; set; }
    public ApprovalStatus ApprovalStatus { get; set; }
    public KycStatus KycStatus { get; set; }
    public DateTime CreatedAt { get; set; }
}
