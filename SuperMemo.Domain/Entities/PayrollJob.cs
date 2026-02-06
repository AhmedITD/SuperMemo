using SuperMemo.Domain.Entities.Common;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Domain.Entities;

public class PayrollJob : BaseEntity
{
    public int EmployeeUserId { get; set; }
    public string? EmployerId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public string? Schedule { get; set; }
    public DateTime? NextRunAt { get; set; }
    public PayrollJobStatus Status { get; set; } = PayrollJobStatus.Active;

    public User EmployeeUser { get; set; } = null!;
}
