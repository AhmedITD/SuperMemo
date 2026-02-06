using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Transactions;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Transactions;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Sinks;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class TransactionService(ISuperMemoDbContext db, IAuditEventLogger auditLogger) : ITransactionService
{
    public async Task<ApiResponse<TransactionResponse>> CreateTransferAsync(CreateTransferRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var fromAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.FromAccountId && a.UserId == userId, cancellationToken);
        if (fromAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Account not found or access denied.", code: ErrorCodes.ResourceNotFound);

        if (fromAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Account is not active for transfers.", code: ErrorCodes.AccountInactive);

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

        var transaction = new Transaction
        {
            FromAccountId = request.FromAccountId,
            ToAccountNumber = request.ToAccountNumber,
            Amount = request.Amount,
            TransactionType = Domain.Enums.TransactionType.Debit,
            Status = TransactionStatus.Sending,
            Purpose = request.Purpose,
            IdempotencyKey = request.IdempotencyKey
        };
        db.Transactions.Add(transaction);
        fromAccount.Balance -= request.Amount;
        toAccount.Balance += request.Amount;
        transaction.Status = TransactionStatus.Completed;

        await db.SaveChangesAsync(cancellationToken);
        await auditLogger.LogAsync("Transaction", transaction.Id.ToString(), "TransferCompleted", new { request.FromAccountId, request.ToAccountNumber, request.Amount }, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, request.FromAccountId));
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
                CreatedAt = t.CreatedAt
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
            TransactionType = Domain.Enums.TransactionType.Debit,
            Status = TransactionStatus.Sending,
            Purpose = purpose,
            IdempotencyKey = idempotencyKey
        };
        db.Transactions.Add(transaction);
        fromAccount.Balance -= amount;
        toAccount.Balance += amount;
        transaction.Status = TransactionStatus.Completed;

        await db.SaveChangesAsync(cancellationToken);
        return ApiResponse<TransactionResponse>.SuccessResponse(Map(transaction, toAccount.Id));
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
            CreatedAt = t.CreatedAt
        };
    }
}
