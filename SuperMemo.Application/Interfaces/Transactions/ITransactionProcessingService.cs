namespace SuperMemo.Application.Interfaces.Transactions;

/// <summary>
/// Service for processing pending transactions in the background.
/// </summary>
public interface ITransactionProcessingService
{
    /// <summary>
    /// Processes pending transactions (moves from Pending to Sending to Completed).
    /// </summary>
    Task<int> ProcessPendingTransactionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Expires transactions that have been in Pending status for too long.
    /// </summary>
    Task<int> ExpirePendingTransactionsAsync(TimeSpan maxPendingDuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Automatically retries failed transactions with temporary failures.
    /// </summary>
    Task<int> AutoRetryFailedTransactionsAsync(int maxRetries, CancellationToken cancellationToken = default);
}
