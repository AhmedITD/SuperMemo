namespace SuperMemo.Application.DTOs.responses.Analytics;

public class AnalyticsTransactionsResponse
{
    public decimal TotalCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
