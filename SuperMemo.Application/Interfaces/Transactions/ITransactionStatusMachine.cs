using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Transactions;

/// <summary>
/// Service for managing transaction status transitions with state machine validation.
/// </summary>
public interface ITransactionStatusMachine
{
    /// <summary>
    /// Validates if a status transition is allowed.
    /// </summary>
    bool IsValidTransition(TransactionStatus fromStatus, TransactionStatus toStatus);
    
    /// <summary>
    /// Transitions a transaction to a new status, updating status_changed_at.
    /// Throws InvalidStatusTransitionException if transition is not allowed.
    /// </summary>
    void TransitionTo(Transaction transaction, TransactionStatus newStatus, int? changedBy = null, string? reason = null);
    
    /// <summary>
    /// Gets all valid next statuses for a given current status.
    /// </summary>
    IReadOnlyList<TransactionStatus> GetValidNextStatuses(TransactionStatus currentStatus);
}
