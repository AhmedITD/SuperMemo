using SuperMemo.Application.DTOs.responses.Cards;
using SuperMemo.Application.DTOs.responses.Transactions;

namespace SuperMemo.Application.DTOs.responses.Dashboard;

public class DashboardResponse
{
    public decimal TotalBalance { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public UserInfoDto User { get; set; } = null!;
    public List<CardResponse> Cards { get; set; } = new();
    public List<TransactionListItemDto> RecentTransactions { get; set; } = new();
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Phone { get; set; } = null!;
    public DateTime AccountCreatedAt { get; set; }
}

public class TransactionListItemDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Direction { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
