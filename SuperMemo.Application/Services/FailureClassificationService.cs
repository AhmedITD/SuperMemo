using SuperMemo.Application.Interfaces.Fraud;
using SuperMemo.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace SuperMemo.Application.Services;

public class FailureClassificationService : IFailureClassificationService
{
    public FailureClassification ClassifyFailure(Exception error, TransactionContext? context = null)
    {
        // Network/timeout errors are temporary
        if (error is TimeoutException || 
            error.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("network", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = true,
                FailureReason = FailureReason.NetworkTimeout,
                Message = "Network timeout occurred. Please retry.",
                RetryAfterSeconds = 10
            };
        }

        // Service unavailable errors are temporary
        if (error.Message.Contains("service unavailable", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("503", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = true,
                FailureReason = FailureReason.ServiceUnavailable,
                Message = "Service temporarily unavailable. Please retry.",
                RetryAfterSeconds = 30
            };
        }

        // Database concurrency conflicts are temporary
        if (error is DbUpdateConcurrencyException ||
            error.Message.Contains("concurrency", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("conflict", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = true,
                FailureReason = FailureReason.ConcurrencyConflict,
                Message = "Transaction conflict. Please retry.",
                RetryAfterSeconds = 5
            };
        }

        // Insufficient funds is permanent
        if (error.Message.Contains("insufficient", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("balance", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = false,
                FailureReason = FailureReason.InsufficientFunds,
                Message = "Insufficient funds."
            };
        }

        // Invalid destination is permanent
        if (error.Message.Contains("destination", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("account not found", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = false,
                FailureReason = FailureReason.InvalidDestination,
                Message = "Invalid destination account."
            };
        }

        // Risk blocked is permanent (unless admin approves)
        if (error.Message.Contains("risk", StringComparison.OrdinalIgnoreCase) ||
            error.Message.Contains("fraud", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = false,
                FailureReason = FailureReason.RiskBlocked,
                Message = "Transaction blocked due to risk assessment."
            };
        }

        // Account frozen/closed is permanent
        if (error.Message.Contains("frozen", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = false,
                FailureReason = FailureReason.AccountFrozen,
                Message = "Account is frozen."
            };
        }

        if (error.Message.Contains("closed", StringComparison.OrdinalIgnoreCase))
        {
            return new FailureClassification
            {
                IsTemporary = false,
                FailureReason = FailureReason.AccountClosed,
                Message = "Account is closed."
            };
        }

        // Default: treat as temporary for unknown errors (can be retried)
        return new FailureClassification
        {
            IsTemporary = true,
            FailureReason = FailureReason.ServiceUnavailable,
            Message = "An error occurred. Please retry.",
            RetryAfterSeconds = 10
        };
    }
}
