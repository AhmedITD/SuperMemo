namespace SuperMemo.Domain.Enums;

/// <summary>
/// Category/type of transaction (what kind of operation it represents).
/// </summary>
public enum TransactionCategory
{
    Transfer = 0,    // Regular money transfer between accounts
    TopUp = 1,       // Wallet top-up via payment gateway
    Withdraw = 2,    // Withdrawal from wallet
    Interest = 3     // Interest payment (for savings accounts)
}
