using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SuperMemo.Api.Common;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Exceptions;

namespace SuperMemo.Api.Controllers;

[Authorize(Policy = "Admin")]
[Route("api/admin/transactions")]
public class AdminTransactionController(
    ITransactionStatusMachine statusMachine,
    ISuperMemoDbContext db) : BaseController
{
    /// <summary>
    /// Lists high-risk transactions pending review.
    /// </summary>
    [HttpGet("risk-review")]
    public async Task<ActionResult<ApiResponse<PaginatedListResponse<Application.DTOs.responses.Transactions.TransactionResponse>>>> GetRiskReviewTransactions(
        [FromQuery] RiskLevel? riskLevel = RiskLevel.High,
        [FromQuery] TransactionStatus? status = TransactionStatus.Pending,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var query = db.Transactions
            .Include(t => t.FromAccount)
            .ThenInclude(a => a.User)
            .Where(t => t.RiskLevel == riskLevel && t.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new Application.DTOs.responses.Transactions.TransactionResponse
            {
                Id = t.Id,
                FromAccountId = t.FromAccountId,
                ToAccountNumber = t.ToAccountNumber,
                Amount = t.Amount,
                TransactionType = "DEBIT",
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

        var response = new PaginatedListResponse<Application.DTOs.responses.Transactions.TransactionResponse>(items, total, pageNumber, pageSize);
        return Ok(ApiResponse<PaginatedListResponse<Application.DTOs.responses.Transactions.TransactionResponse>>.SuccessResponse(response));
    }

    /// <summary>
    /// Approves or rejects a high-risk transaction.
    /// </summary>
    [HttpPost("{transactionId}/review")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>>> ReviewTransaction(
        int transactionId,
        [FromBody] ReviewTransactionRequest request,
        CancellationToken cancellationToken = default)
    {
        var transaction = await db.Transactions
            .Include(t => t.FromAccount)
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);

        if (transaction == null)
            return NotFound(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.ErrorResponse(
                "Transaction not found.", code: ErrorCodes.ResourceNotFound));

        if (transaction.Status != TransactionStatus.Pending)
            return BadRequest(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.ErrorResponse(
                "Transaction is not in Pending status.", code: ErrorCodes.ValidationFailed));

        try
        {
            if (request.Action == "approve")
            {
                // Move to Sending and process
                statusMachine.TransitionTo(transaction, TransactionStatus.Sending, null, $"Admin approved: {request.Reason}");

                var fromAccount = transaction.FromAccount;
                var toAccount = await db.Accounts.FirstOrDefaultAsync(a => a.AccountNumber == transaction.ToAccountNumber, cancellationToken);
                if (toAccount == null)
                    throw new Exception("Destination account not found.");

                if (fromAccount.Balance < transaction.Amount)
                    throw new Exception("Insufficient balance.");

                fromAccount.Balance -= transaction.Amount;
                toAccount.Balance += transaction.Amount;
                statusMachine.TransitionTo(transaction, TransactionStatus.Completed, null, "Admin approved and processed");

                await db.SaveChangesAsync(cancellationToken);

                var response = new Application.DTOs.responses.Transactions.TransactionResponse
                {
                    Id = transaction.Id,
                    FromAccountId = transaction.FromAccountId,
                    ToAccountNumber = transaction.ToAccountNumber,
                    Amount = transaction.Amount,
                    TransactionType = "DEBIT",
                    Status = transaction.Status,
                    Purpose = transaction.Purpose,
                    IdempotencyKey = transaction.IdempotencyKey,
                    CreatedAt = transaction.CreatedAt,
                    FailureReason = transaction.FailureReason,
                    RetryCount = transaction.RetryCount,
                    RiskScore = transaction.RiskScore,
                    RiskLevel = transaction.RiskLevel,
                    StatusChangedAt = transaction.StatusChangedAt
                };

                return Ok(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.SuccessResponse(response));
            }
            else if (request.Action == "reject")
            {
                statusMachine.TransitionTo(transaction, TransactionStatus.Failed, null, $"Admin rejected: {request.Reason}");
                transaction.FailureReason = FailureReason.RiskBlocked;

                await db.SaveChangesAsync(cancellationToken);

                var response = new Application.DTOs.responses.Transactions.TransactionResponse
                {
                    Id = transaction.Id,
                    FromAccountId = transaction.FromAccountId,
                    ToAccountNumber = transaction.ToAccountNumber,
                    Amount = transaction.Amount,
                    TransactionType = "DEBIT",
                    Status = transaction.Status,
                    Purpose = transaction.Purpose,
                    IdempotencyKey = transaction.IdempotencyKey,
                    CreatedAt = transaction.CreatedAt,
                    FailureReason = transaction.FailureReason,
                    RetryCount = transaction.RetryCount,
                    RiskScore = transaction.RiskScore,
                    RiskLevel = transaction.RiskLevel,
                    StatusChangedAt = transaction.StatusChangedAt
                };

                return Ok(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.SuccessResponse(response));
            }
            else
            {
                return BadRequest(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.ErrorResponse(
                    "Invalid action. Must be 'approve' or 'reject'.", code: ErrorCodes.ValidationFailed));
            }
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>.ErrorResponse(
                $"Error processing review: {ex.Message}", code: ErrorCodes.ValidationFailed));
        }
    }
}

/// <summary>
/// Request model for transaction review.
/// </summary>
public class ReviewTransactionRequest
{
    public required string Action { get; set; } // "approve" | "reject"
    public string? Reason { get; set; }
}
