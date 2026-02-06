using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace SuperMemo.Application.Services;

public class TransactionStatusMachine(ISuperMemoDbContext db) : ITransactionStatusMachine
{
    // Define valid state transitions
    private static readonly Dictionary<TransactionStatus, HashSet<TransactionStatus>> ValidTransitions = new()
    {
        { TransactionStatus.Created, new HashSet<TransactionStatus> { TransactionStatus.Pending, TransactionStatus.Failed } },
        { TransactionStatus.Pending, new HashSet<TransactionStatus> { TransactionStatus.Sending, TransactionStatus.Failed, TransactionStatus.Expired } },
        { TransactionStatus.Sending, new HashSet<TransactionStatus> { TransactionStatus.Completed, TransactionStatus.Failed } },
        { TransactionStatus.Completed, new HashSet<TransactionStatus>() }, // Terminal state
        { TransactionStatus.Failed, new HashSet<TransactionStatus>() }, // Terminal state
        { TransactionStatus.Expired, new HashSet<TransactionStatus>() } // Terminal state
    };

    public bool IsValidTransition(TransactionStatus fromStatus, TransactionStatus toStatus)
    {
        if (fromStatus == toStatus)
            return true; // No-op transition is allowed

        return ValidTransitions.TryGetValue(fromStatus, out var allowed) && allowed.Contains(toStatus);
    }

    public void TransitionTo(Transaction transaction, TransactionStatus newStatus, int? changedBy = null, string? reason = null)
    {
        var currentStatus = transaction.Status;
        
        if (!IsValidTransition(currentStatus, newStatus))
        {
            throw new InvalidStatusTransitionException(
                $"Invalid status transition from {currentStatus} to {newStatus}.");
        }

        // Update status and timestamp
        transaction.Status = newStatus;
        transaction.StatusChangedAt = DateTime.UtcNow;

        // Log to history table (only if transaction has an ID - i.e., already saved)
        if (transaction.Id > 0)
        {
            var history = new TransactionStatusHistory
            {
                TransactionId = transaction.Id,
                OldStatus = currentStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedBy = changedBy,
                Reason = reason
            };
            db.TransactionStatusHistory.Add(history);
        }
    }

    public IReadOnlyList<TransactionStatus> GetValidNextStatuses(TransactionStatus currentStatus)
    {
        if (ValidTransitions.TryGetValue(currentStatus, out var allowed))
            return allowed.ToList();
        return new List<TransactionStatus>();
    }
}
