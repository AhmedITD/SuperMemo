using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Payroll;

public class PayrollJobResponse
{
    public int Id { get; set; }
    public int EmployeeUserId { get; set; }
    public string? EmployerId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = null!;
    public string? Schedule { get; set; }
    public DateTime? NextRunAt { get; set; }
    public PayrollJobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
