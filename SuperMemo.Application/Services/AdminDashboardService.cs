using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Admin;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Admin;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AdminDashboardService(ISuperMemoDbContext db) : IAdminDashboardService
{
    public async Task<ApiResponse<AdminDashboardMetricsResponse>> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var activeUsersCount = await db.Users
            .CountAsync(u => u.ApprovalStatus == ApprovalStatus.Approved, cancellationToken);

        var pendingUsersCount = await db.Users
            .CountAsync(u => u.ApprovalStatus == ApprovalStatus.PendingApproval, cancellationToken);

        var rejectedUsersCount = await db.Users
            .CountAsync(u => u.ApprovalStatus == ApprovalStatus.Rejected, cancellationToken);

        var totalUsersCount = await db.Users
            .CountAsync(cancellationToken);

        var response = new AdminDashboardMetricsResponse
        {
            ActiveUsersCount = activeUsersCount,
            PendingUsersCount = pendingUsersCount,
            RejectedUsersCount = rejectedUsersCount,
            TotalUsersCount = totalUsersCount
        };

        return ApiResponse<AdminDashboardMetricsResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<PaginatedListResponse<UserListItemResponse>>> GetUsersAsync(
        string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsQueryable();

        // Search by name
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => u.FullName.Contains(search));
        }

        // Filter by status
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<ApprovalStatus>(status, true, out var approvalStatus))
            {
                query = query.Where(u => u.ApprovalStatus == approvalStatus);
            }
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemResponse
            {
                Id = u.Id,
                FullName = u.FullName,
                Phone = u.Phone,
                Role = u.Role,
                ApprovalStatus = u.ApprovalStatus,
                KycStatus = u.KycStatus,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PaginatedListResponse<UserListItemResponse>(users, totalCount, page, pageSize);

        return ApiResponse<PaginatedListResponse<UserListItemResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<UserStatusResponse>> GetUserStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await db.Users
            .Include(u => u.Account)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
            return ApiResponse<UserStatusResponse>.ErrorResponse("User not found.", code: ErrorCodes.ResourceNotFound);

        var statusDescription = user.ApprovalStatus switch
        {
            ApprovalStatus.Approved => "Active",
            ApprovalStatus.PendingApproval => "Pending",
            ApprovalStatus.Rejected => "Rejected",
            _ => "Unknown"
        };

        var response = new UserStatusResponse
        {
            UserId = user.Id,
            ApprovalStatus = user.ApprovalStatus,
            KycStatus = user.KycStatus,
            KybStatus = user.KybStatus,
            AccountStatus = user.Account?.Status,
            StatusDescription = statusDescription
        };

        return ApiResponse<UserStatusResponse>.SuccessResponse(response);
    }
}
