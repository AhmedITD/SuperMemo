using SuperMemo.Application.DTOs.requests.Payroll;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payroll;

namespace SuperMemo.Application.Interfaces.Payroll;

public interface IPayrollJobService
{
    Task<ApiResponse<PayrollJobResponse>> CreateAsync(CreatePayrollJobRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PayrollJobResponse>> UpdateAsync(int jobId, UpdatePayrollJobRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<PayrollJobResponse>> GetByIdAsync(int jobId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaginatedListResponse<PayrollJobResponse>>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> DeleteAsync(int jobId, CancellationToken cancellationToken = default);
}
