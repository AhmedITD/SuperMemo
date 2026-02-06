using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.requests.Payroll;

public class UpdatePayrollJobRequest
{
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string? Schedule { get; set; }
    public DateTime? NextRunAt { get; set; }
    public PayrollJobStatus? Status { get; set; }
}
