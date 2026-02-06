using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Accounts;

public class AccountResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AccountNumber { get; set; } = null!;
    public decimal Balance { get; set; }
    public string Currency { get; set; } = null!;
    public AccountStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
