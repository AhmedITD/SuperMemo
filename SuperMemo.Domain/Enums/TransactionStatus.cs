namespace SuperMemo.Domain.Enums;

public enum TransactionStatus
{
    Created = 0,
    Pending = 1,
    Sending = 2,
    Completed = 3,
    Failed = 4,
    Expired = 5
}
