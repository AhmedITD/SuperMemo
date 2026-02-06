namespace SuperMemo.Application.DTOs.responses.Analytics;

public class AnalyticsBalanceTrendResponse
{
    public List<MonthlyBalanceDto> MonthlyBalances { get; set; } = new();
}

public class MonthlyBalanceDto
{
    public string Month { get; set; } = null!; // Format: "YYYY-MM"
    public decimal Balance { get; set; }
}
