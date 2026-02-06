using SuperMemo.Application.DTOs.responses.Analytics;
using SuperMemo.Application.DTOs.responses.Common;

namespace SuperMemo.Application.Interfaces.Analytics;

public interface IAnalyticsService
{
    Task<ApiResponse<AnalyticsOverviewResponse>> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<AnalyticsTransactionsResponse>> GetTransactionsAsync(int userId, DateTime? startDate, DateTime? endDate, string? direction, CancellationToken cancellationToken = default);
    Task<ApiResponse<AnalyticsBalanceTrendResponse>> GetBalanceTrendAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaginatedListResponse<TransactionListItemResponse>>> GetTransactionsListAsync(int userId, int page, int pageSize, DateTime? startDate, DateTime? endDate, CancellationToken cancellationToken = default);
}
