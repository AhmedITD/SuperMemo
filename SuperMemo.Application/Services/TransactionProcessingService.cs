using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Fraud;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class TransactionProcessingService(
    ISuperMemoDbContext db,
    ITransactionStatusMachine statusMachine,
    IFailureClassificationService failureClassificationService) : ITransactionProcessingService
{
    public async Task<int> ProcessPendingTransactionsAsync(CancellationToken cancellationToken = default)
    {
        // Get pending transactions that are not high risk (or have been approved)
        var pendingTransactions = await db.Transactions
            .Include(t => t.FromAccount)
            .Where(t => t.Status == TransactionStatus.Pending
                && (t.RiskLevel == null || t.RiskLevel == RiskLevel.Low || t.RiskLevel == RiskLevel.Medium))
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var transaction in pendingTransactions)
        {
            try
            {
                var toAccount = await db.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == transaction.ToAccountNumber, cancellationToken);

                if (toAccount == null)
                {
                    statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, "Destination account not found");
                    transaction.FailureReason = FailureReason.InvalidDestination;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (transaction.FromAccount.Balance < transaction.Amount)
                {
                    statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, "Insufficient balance");
                    transaction.FailureReason = FailureReason.InsufficientFunds;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                // Move to Sending
                statusMachine.TransitionTo(transaction, TransactionStatus.Sending);

                // Execute transfer
                transaction.FromAccount.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;

                // Move to Completed
                statusMachine.TransitionTo(transaction, TransactionStatus.Completed);

                await db.SaveChangesAsync(cancellationToken);
                processed++;
            }
            catch (Exception ex)
            {
                var classification = failureClassificationService.ClassifyFailure(ex);
                statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, classification.Message);
                transaction.FailureReason = classification.FailureReason;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return processed;
    }

    public async Task<int> ExpirePendingTransactionsAsync(TimeSpan maxPendingDuration, CancellationToken cancellationToken = default)
    {
        var expirationThreshold = DateTime.UtcNow - maxPendingDuration;

        var expiredTransactions = await db.Transactions
            .Where(t => t.Status == TransactionStatus.Pending
                && t.StatusChangedAt.HasValue
                && t.StatusChangedAt <= expirationThreshold)
            .ToListAsync(cancellationToken);

        var expired = 0;
        foreach (var transaction in expiredTransactions)
        {
            statusMachine.TransitionTo(transaction, TransactionStatus.Expired, null, "Transaction expired due to timeout");
            transaction.FailureReason = FailureReason.TransactionExpired;
            expired++;
        }

        if (expired > 0)
            await db.SaveChangesAsync(cancellationToken);

        return expired;
    }

    public async Task<int> AutoRetryFailedTransactionsAsync(int maxRetries, CancellationToken cancellationToken = default)
    {
        var retryableTransactions = await db.Transactions
            .Include(t => t.FromAccount)
            .Where(t => t.Status == TransactionStatus.Failed
                && t.RetryCount < maxRetries
                && (t.FailureReason == FailureReason.NetworkTimeout
                    || t.FailureReason == FailureReason.ServiceUnavailable
                    || t.FailureReason == FailureReason.ConcurrencyConflict))
            .ToListAsync(cancellationToken);

        var retried = 0;
        foreach (var transaction in retryableTransactions)
        {
            try
            {
                transaction.RetryCount++;

                var toAccount = await db.Accounts
                    .FirstOrDefaultAsync(a => a.AccountNumber == transaction.ToAccountNumber, cancellationToken);

                if (toAccount == null)
                {
                    statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, "Destination account not found");
                    transaction.FailureReason = FailureReason.InvalidDestination;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                if (transaction.FromAccount.Balance < transaction.Amount)
                {
                    statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, "Insufficient balance");
                    transaction.FailureReason = FailureReason.InsufficientFunds;
                    await db.SaveChangesAsync(cancellationToken);
                    continue;
                }

                // Reset and retry
                statusMachine.TransitionTo(transaction, TransactionStatus.Pending, null, $"Auto-retry attempt {transaction.RetryCount}");
                statusMachine.TransitionTo(transaction, TransactionStatus.Sending);

                transaction.FromAccount.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;

                statusMachine.TransitionTo(transaction, TransactionStatus.Completed);
                transaction.FailureReason = null; // Clear failure reason on success

                await db.SaveChangesAsync(cancellationToken);
                retried++;
            }
            catch (Exception ex)
            {
                var classification = failureClassificationService.ClassifyFailure(ex);
                statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, classification.Message);
                transaction.FailureReason = classification.FailureReason;
                await db.SaveChangesAsync(cancellationToken);
            }
        }

        return retried;
    }
}
