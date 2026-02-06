using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Payroll;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payroll;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Payroll;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class PayrollJobService(ISuperMemoDbContext db) : IPayrollJobService
{
    public async Task<ApiResponse<PayrollJobResponse>> CreateAsync(CreatePayrollJobRequest request, CancellationToken cancellationToken = default)
    {
        var user = await db.Users.Include(u => u.Account).FirstOrDefaultAsync(u => u.Id == request.EmployeeUserId, cancellationToken);
        if (user?.Account == null)
            return ApiResponse<PayrollJobResponse>.ErrorResponse("Employee user or account not found.");

        var job = new PayrollJob
        {
            EmployeeUserId = request.EmployeeUserId,
            EmployerId = request.EmployerId,
            Amount = request.Amount,
            Currency = request.Currency,
            Schedule = request.Schedule,
            NextRunAt = request.NextRunAt,
            Status = PayrollJobStatus.Active
        };
        db.PayrollJobs.Add(job);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<PayrollJobResponse>.SuccessResponse(Map(job));
    }

    public async Task<ApiResponse<PayrollJobResponse>> UpdateAsync(int jobId, UpdatePayrollJobRequest request, CancellationToken cancellationToken = default)
    {
        var job = await db.PayrollJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
            return ApiResponse<PayrollJobResponse>.ErrorResponse("Payroll job not found.");

        if (request.Amount.HasValue) job.Amount = request.Amount.Value;
        if (request.Currency != null) job.Currency = request.Currency;
        if (request.Schedule != null) job.Schedule = request.Schedule;
        if (request.NextRunAt.HasValue) job.NextRunAt = request.NextRunAt;
        if (request.Status.HasValue) job.Status = request.Status.Value;

        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<PayrollJobResponse>.SuccessResponse(Map(job));
    }

    public async Task<ApiResponse<PayrollJobResponse>> GetByIdAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await db.PayrollJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
            return ApiResponse<PayrollJobResponse>.ErrorResponse("Payroll job not found.");
        return ApiResponse<PayrollJobResponse>.SuccessResponse(Map(job));
    }

    public async Task<ApiResponse<PaginatedListResponse<PayrollJobResponse>>> ListAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.PayrollJobs.OrderByDescending(j => j.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new PayrollJobResponse
            {
                Id = j.Id,
                EmployeeUserId = j.EmployeeUserId,
                EmployerId = j.EmployerId,
                Amount = j.Amount,
                Currency = j.Currency,
                Schedule = j.Schedule,
                NextRunAt = j.NextRunAt,
                Status = j.Status,
                CreatedAt = j.CreatedAt
            })
            .ToListAsync(cancellationToken);
        var response = new PaginatedListResponse<PayrollJobResponse>(items, total, pageNumber, pageSize);
        return ApiResponse<PaginatedListResponse<PayrollJobResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<object>> DeleteAsync(int jobId, CancellationToken cancellationToken = default)
    {
        var job = await db.PayrollJobs.FindAsync([jobId], cancellationToken);
        if (job == null)
            return ApiResponse<object>.ErrorResponse("Payroll job not found.", code: ErrorCodes.ResourceNotFound);
        db.PayrollJobs.Remove(job);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { });
    }

    private static PayrollJobResponse Map(PayrollJob j)
    {
        return new PayrollJobResponse
        {
            Id = j.Id,
            EmployeeUserId = j.EmployeeUserId,
            EmployerId = j.EmployerId,
            Amount = j.Amount,
            Currency = j.Currency,
            Schedule = j.Schedule,
            NextRunAt = j.NextRunAt,
            Status = j.Status,
            CreatedAt = j.CreatedAt
        };
    }
}
