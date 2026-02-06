using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Admin;

public class ApproveOrRejectUserRequest
{
    public ApprovalStatus ApprovalStatus { get; set; }
}
