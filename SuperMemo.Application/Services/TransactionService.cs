using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Transactions;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Transactions;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Fraud;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Exceptions;

namespace SuperMemo.Application.Services;

public class TransactionService(
    ISuperMemoDbContext db,
    IAuditEventLogger auditLogger,
    ITransactionStatusMachine statusMachine,
    IFraudDetectionService fraudDetectionService,
    IFailureClassificationService failureClassificationService,
    Application.Interfaces.Accounts.IDailyLimitService dailyLimitService) : ITransactionService
{
    private const int MaxRetries = 3;

    public async Task<ApiResponse<TransactionResponse>> CreateTransferAsync(CreateTransferRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var fromAccount = await db.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId, cancellationToken);
        if (fromAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Account not found or access denied.", code: ErrorCodes.ResourceNotFound);

        if (fromAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Account is not active for transfers.", code: ErrorCodes.AccountInactive);

        // Check user approval and KYC
        if (fromAccount.User.ApprovalStatus != ApprovalStatus.Approved)
            return ApiResponse<TransactionResponse>.ErrorResponse("User is not approved for transactions.", code: ErrorCodes.UserNotApproved);

        var utcNow = DateTime.UtcNow;
        var hasValidCard = await db.Cards.AnyAsync(c =>
            c.AccountId == request.FromAccountId
            && c.IsActive
            && !c.IsExpired
            && c.ExpiryDate >= utcNow.Date, cancellationToken);
        if (!hasValidCard)
            return ApiResponse<TransactionResponse>.ErrorResponse("No active, non-expired card on this account. Transfers require at least one valid card.", code: ErrorCodes.NoActiveCard);

        if (request.Amount <= 0)
            return ApiResponse<TransactionResponse>.ErrorResponse("Amount must be positive.");

        // Idempotency check
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await db.Transactions
                .FirstOrDefaultAsync(t => t.FromAccountId == request.FromAccountId && t.IdempotencyKey == request.IdempotencyKey, cancellationToken);
            if (existing != null)
                return ApiResponse<TransactionResponse>.SuccessResponse(Map(existing, request.FromAccountId));
        }

        var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber, cancellationToken);
        if (toAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account not found.", code: ErrorCodes.DestinationAccountNotFound);

        if (toAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account is not active.", code: ErrorCodes.AccountInactive);

        if (fromAccount.Balance < request.Amount)
            return ApiResponse<TransactionResponse>.ErrorResponse("Insufficient balance.", code: ErrorCodes.InsufficientFunds);

        // Check daily spending limit for Savings accounts
        if (fromAccount.AccountType == Domain.Enums.AccountType.Savings)
        {
            var canSpend = await dailyLimitService.CheckDailyLimitAsync(fromAccount.Id, request.Amount, cancellationToken);
            if (!canSpend)
            {
                var remaining = await dailyLimitService.GetRemainingLimitAsync(fromAccount.Id, cancellationToken);
                return ApiResponse<TransactionResponse>.ErrorResponse(
                    $"Daily spending limit exceeded. Remaining limit: {remaining:C}",
                    code: ErrorCodes.DailyLimitExceeded);
            }
        }

        // Create transaction with Created status
        var transaction = new Transaction
        {
            FromAccountId = request.FromAccountId,
            ToAccountNumber = request.ToAccountNumber,
            Amount = request.Amount,
            TransactionType = TransactionType.Debit,
            Status = TransactionStatus.Created,
            Purpose = request.Purpose,
            IdempotencyKey = request.IdempotencyKey,
            StatusChangedAt = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);
        await db.SaveChangesAsync(cancellationToken); // Save to get transaction ID

        try
        {
            // Run fraud detection
            var fraudResult = await fraudDetectionService.CalculateRiskScoreAsync(
                transaction, fromAccount.User, fromAccount, null, cancellationToken);

            transaction.RiskScore = fraudResult.RiskScore;
            transaction.RiskLevel = fraudResult.RiskLevel;

            // Move to Pending status
            statusMachine.TransitionTo(transaction, TransactionStatus.Pending);

            // If HIGH risk, keep in Pending for admin review (don't proceed to Sending)
            if (fraudResult.RiskLevel == RiskLevel.High)
            {
                await db.SaveChangesAsync(cancellationToken);
                await auditLogger.LogAsync("Transaction", transaction.Id.ToString(), "TransferCreatedHighRisk",
                    new { request.FromAccountId, request.ToAccountNumber, request.Amount, fraudResult.RiskScore, fraudResult.RiskLevel }, cancellationToken);
                return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, request.FromAccountId));
            }

            // For LOW/MEDIUM risk, proceed to processing (will be handled by background job)
            // Or process immediately if synchronous processing is desired
            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("Transaction", transaction.Id.ToString(), "TransferCreated",
                new { request.FromAccountId, request.ToAccountNumber, request.Amount }, cancellationToken);

            return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, request.FromAccountId));
        }
        catch (Exception ex)
        {
            // Classify failure
            var classification = failureClassificationService.ClassifyFailure(ex);
            transaction.FailureReason = classification.FailureReason;
            statusMachine.TransitionTo(transaction, TransactionStatus.Failed);

            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("Transaction", transaction.Id.ToString(), "TransferFailed",
                new { request.FromAccountId, request.ToAccountNumber, request.Amount, classification.FailureReason }, cancellationToken);

            var response = Map(transaction, request.FromAccountId);
            response.RetryRecommended = classification.IsTemporary;
            response.RetryAfterSeconds = classification.RetryAfterSeconds;
            response.RetryCount = transaction.RetryCount;
            response.MaxRetries = MaxRetries;

            return ApiResponse<TransactionResponse>.ErrorResponse(
                classification.Message ?? "Transaction failed.",
                code: classification.IsTemporary ? ErrorCodes.TemporaryFailure : ErrorCodes.PermanentFailure);
        }
    }

    public async Task<ApiResponse<PaginatedListResponse<TransactionResponse>>> ListByAccountAsync(int accountId, int userId, TransactionStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts.Where(a => a.Id == accountId && a.UserId == userId).Select(a => new { a.AccountNumber }).FirstOrDefaultAsync(cancellationToken);
        if (account == null)
            return ApiResponse<PaginatedListResponse<TransactionResponse>>.ErrorResponse("Account not found or access denied.", code: ErrorCodes.ResourceNotFound);

        var accountNumber = account.AccountNumber;

        var query = db.Transactions.Where(t => t.FromAccountId == accountId || t.ToAccountNumber == accountNumber);
        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionResponse
            {
                Id = t.Id,
                FromAccountId = t.FromAccountId,
                ToAccountNumber = t.ToAccountNumber,
                Amount = t.Amount,
                TransactionType = t.FromAccountId == accountId ? "DEBIT" : "CREDIT",
                Status = t.Status,
                Purpose = t.Purpose,
                IdempotencyKey = t.IdempotencyKey,
                CreatedAt = t.CreatedAt,
                FailureReason = t.FailureReason,
                RetryCount = t.RetryCount,
                RiskScore = t.RiskScore,
                RiskLevel = t.RiskLevel,
                StatusChangedAt = t.StatusChangedAt
            })
            .ToListAsync(cancellationToken);

        var response = new PaginatedListResponse<TransactionResponse>(items, total, pageNumber, pageSize);
        return ApiResponse<PaginatedListResponse<TransactionResponse>>.SuccessResponse(response);
    }

    public async Task<ApiResponse<TransactionResponse>> GetByIdAsync(int transactionId, int userId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions.Include(t => t.FromAccount).FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
        if (transaction == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Transaction not found.", code: ErrorCodes.ResourceNotFound);

        int forAccountId;
        if (transaction.FromAccount.UserId == userId)
            forAccountId = transaction.FromAccountId;
        else
        {
            var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == transaction.ToAccountNumber && a.UserId == userId, cancellationToken);
            if (toAccount == null)
                return ApiResponse<TransactionResponse>.ErrorResponse("Transaction not found.", code: ErrorCodes.ResourceNotFound);
            forAccountId = toAccount.Id;
        }
        return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, forAccountId));
    }

    public async Task<ApiResponse<TransactionResponse>> CreatePayrollCreditAsync(int fromAccountId, string toAccountNumber, decimal amount, string idempotencyKey, string? purpose, CancellationToken cancellationToken = default)
    {
        var fromAccount = await db.Accounts.FirstOrDefaultAsync(a => a.Id == fromAccountId, cancellationToken);
        if (fromAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Payroll source account not found.", code: ErrorCodes.ResourceNotFound);
        if (fromAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Payroll source account is not active.", code: ErrorCodes.AccountInactive);

        var existing = await db.Transactions.FirstOrDefaultAsync(t => t.FromAccountId == fromAccountId && t.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing != null)
        {
            var toAccountForExisting = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == existing.ToAccountNumber, cancellationToken);
            var forAccountId = toAccountForExisting?.Id ?? existing.FromAccountId;
            return ApiResponse<TransactionResponse>.SuccessResponse(Map(existing, forAccountId));
        }

        var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == toAccountNumber, cancellationToken);
        if (toAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account not found.", code: ErrorCodes.DestinationAccountNotFound);
        if (toAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account is not active.", code: ErrorCodes.AccountInactive);

        var transaction = new Transaction
        {
            FromAccountId = fromAccountId,
            ToAccountNumber = toAccountNumber,
            Amount = amount,
            TransactionType = TransactionType.Debit,
            Status = TransactionStatus.Created,
            Purpose = purpose,
            IdempotencyKey = idempotencyKey,
            StatusChangedAt = DateTime.UtcNow
        };
        db.Transactions.Add(transaction);
        
        // Payroll transactions skip fraud checks and go directly to processing
        statusMachine.TransitionTo(transaction, TransactionStatus.Pending);
        statusMachine.TransitionTo(transaction, TransactionStatus.Sending);
        
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;
        statusMachine.TransitionTo(transaction, TransactionStatus.Completed);

        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, toAccount.Id));
    }

    /// <summary>
    /// Retries a failed transaction if it's eligible for retry.
    /// </summary>
    public async Task<ApiResponse<TransactionResponse>> RetryTransactionAsync(int transactionId, int userId, CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions
            .Include(t => t.FromAccount)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Transaction not found.", code: ErrorCodes.ResourceNotFound);

        // Verify ownership
        if (transaction.FromAccount.UserId != userId)
            return ApiResponse<TransactionResponse>.ErrorResponse("Access denied.", code: ErrorCodes.ResourceNotFound);

        // Check if transaction is in Failed state
        if (transaction.Status != TransactionStatus.Failed)
            return ApiResponse<TransactionResponse>.ErrorResponse("Transaction is not in Failed state.", code: ErrorCodes.ValidationFailed);

        // Check if failure was temporary
        if (transaction.FailureReason == null || 
            (transaction.FailureReason != FailureReason.NetworkTimeout &&
             transaction.FailureReason != FailureReason.ServiceUnavailable &&
             transaction.FailureReason != FailureReason.ConcurrencyConflict))
        {
            return ApiResponse<TransactionResponse>.ErrorResponse("Transaction cannot be retried (permanent failure).", code: ErrorCodes.PermanentFailure);
        }

        // Check retry count
        if (transaction.RetryCount >= MaxRetries)
            return ApiResponse<TransactionResponse>.ErrorResponse($"Maximum retry attempts ({MaxRetries}) exceeded.", code: ErrorCodes.ValidationFailed);

        // Increment retry count
        transaction.RetryCount++;

        try
        {
            // Reset status and retry
            statusMachine.TransitionTo(transaction, TransactionStatus.Pending);

            var fromAccount = transaction.FromAccount;
            var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == transaction.ToAccountNumber, cancellationToken);
            if (toAccount == null)
                throw new Exception("Destination account not found.");

            if (fromAccount.Balance < transaction.Amount)
                throw new Exception("Insufficient balance.");

            statusMachine.TransitionTo(transaction, TransactionStatus.Sending);
            fromAccount.Balance -= transaction.Amount;
            toAccount.Balance += transaction.Amount;
            statusMachine.TransitionTo(transaction, TransactionStatus.Completed);
            transaction.FailureReason = null; // Clear failure reason on success

            await db.SaveChangesAsync(cancellationToken);
            await auditLogger.LogAsync("Transaction", transaction.Id.ToString(), "TransferRetried",
                new { transactionId, retryCount = transaction.RetryCount }, cancellationToken);

            return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, fromAccount.Id));
        }
        catch (Exception ex)
        {
            var classification = failureClassificationService.ClassifyFailure(ex);
            transaction.FailureReason = classification.FailureReason;
            statusMachine.TransitionTo(transaction, TransactionStatus.Failed);

            await db.SaveChangesAsync(cancellationToken);

            var response = Map(transaction, transaction.FromAccountId);
            response.RetryRecommended = classification.IsTemporary && transaction.RetryCount < MaxRetries;
            response.RetryAfterSeconds = classification.RetryAfterSeconds;
            response.RetryCount = transaction.RetryCount;
            response.MaxRetries = MaxRetries;

            return ApiResponse<TransactionResponse>.ErrorResponse(
                classification.Message ?? "Retry failed.",
                code: classification.IsTemporary ? ErrorCodes.TemporaryFailure : ErrorCodes.PermanentFailure);
        }
    }

    private static TransactionResponse Map(Transaction t, int forAccountId)
    {
        return new TransactionResponse
        {
            Id = t.Id,
            FromAccountId = t.FromAccountId,
            ToAccountNumber = t.ToAccountNumber,
            Amount = t.Amount,
            TransactionType = t.FromAccountId == forAccountId ? "DEBIT" : "CREDIT",
            Status = t.Status,
            Purpose = t.Purpose,
            IdempotencyKey = t.IdempotencyKey,
            CreatedAt = t.CreatedAt,
            FailureReason = t.FailureReason,
            RetryCount = t.RetryCount,
            RiskScore = t.RiskScore,
            RiskLevel = t.RiskLevel,
            StatusChangedAt = t.StatusChangedAt
        };
    }
}
