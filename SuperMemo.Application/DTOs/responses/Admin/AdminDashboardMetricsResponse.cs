namespace SuperMemo.Application.DTOs.responses.Admin;

public class AdminDashboardMetricsResponse
{
    public int ActiveUsersCount { get; set; }
    public int PendingUsersCount { get; set; }
    public int RejectedUsersCount { get; set; }
    public int TotalUsersCount { get; set; }
}
