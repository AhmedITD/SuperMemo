using SuperMemo.Application.DTOs.responses.Admin;
using SuperMemo.Application.DTOs.responses.Common;

namespace SuperMemo.Application.Interfaces.Admin;

public interface IAdminDashboardService
{
    Task<ApiResponse<AdminDashboardMetricsResponse>> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<ApiResponse<PaginatedListResponse<UserListItemResponse>>> GetUsersAsync(string? search, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserStatusResponse>> GetUserStatusAsync(int userId, CancellationToken cancellationToken = default);
}
