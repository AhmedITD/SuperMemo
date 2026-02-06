using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Dashboard;

namespace SuperMemo.Application.Interfaces.Dashboard;

public interface IDashboardService
{
    Task<ApiResponse<DashboardResponse>> GetDashboardAsync(int userId, CancellationToken cancellationToken = default);
}
