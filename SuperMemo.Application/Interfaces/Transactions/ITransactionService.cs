using SuperMemo.Application.DTOs.requests.Transactions;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Transactions;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Transactions;

public interface ITransactionService
{
    Task<ApiResponse<TransactionResponse>> CreateTransferAsync(CreateTransferRequest request, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaginatedListResponse<TransactionResponse>>> ListByAccountAsync(int accountId, int userId, TransactionStatus? status, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<ApiResponse<TransactionResponse>> GetByIdAsync(int transactionId, int userId, CancellationToken cancellationToken = default);
    /// <summary>Internal use by payroll runner only. Creates a credit from system account to employee account (no user/balance check on from account).</summary>
    Task<ApiResponse<TransactionResponse>> CreatePayrollCreditAsync(int fromAccountId, string toAccountNumber, decimal amount, string idempotencyKey, string? purpose, CancellationToken cancellationToken = default);
    /// <summary>Retries a failed transaction if it's eligible for retry.</summary>
    Task<ApiResponse<TransactionResponse>> RetryTransactionAsync(int transactionId, int userId, CancellationToken cancellationToken = default);
}
