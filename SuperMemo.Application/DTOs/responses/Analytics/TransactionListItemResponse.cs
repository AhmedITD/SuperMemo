using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.DTOs.responses.Analytics;

public class TransactionListItemResponse
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public TransactionType TransactionType { get; set; }
    public TransactionStatus Status { get; set; }
    public string? Purpose { get; set; }
    public string ToAccountNumber { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
