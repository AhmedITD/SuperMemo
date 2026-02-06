namespace SuperMemo.Application.DTOs.requests.Payroll;

public class CreatePayrollJobRequest
{
    public int EmployeeUserId { get; set; }
    public string? EmployerId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public string? Schedule { get; set; }
    public DateTime? NextRunAt { get; set; }
}
