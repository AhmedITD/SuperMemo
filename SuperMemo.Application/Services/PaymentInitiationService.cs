using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payments;
using SuperMemo.Application.DTOs.responses.Transactions;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Payments;
using SuperMemo.Application.Interfaces.Transactions;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class PaymentInitiationService(
    ISuperMemoDbContext db,
    ITransactionService transactionService) : IPaymentInitiationService
{
    public async Task<ApiResponse<QrCodeResponse>> GenerateQrCodeAsync(string accountNumber, CancellationToken cancellationToken = default)
    {
        var account = await db.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber, cancellationToken);

        if (account == null)
            return ApiResponse<QrCodeResponse>.ErrorResponse("Account not found.", code: ErrorCodes.ResourceNotFound);

        if (account.Status != AccountStatus.Active)
            return ApiResponse<QrCodeResponse>.ErrorResponse("Account is not active.", code: ErrorCodes.AccountInactive);

        // Check if merchant account exists
        var merchantAccount = await db.MerchantAccounts
            .FirstOrDefaultAsync(m => m.AccountId == account.Id && m.IsActive, cancellationToken);

        string merchantId;
        string merchantName;

        if (merchantAccount != null)
        {
            merchantId = merchantAccount.MerchantId;
            merchantName = merchantAccount.MerchantName;
        }
        else
        {
            // Generate merchant ID if not exists
            merchantId = $"M{account.Id:D6}";
            merchantName = account.User.FullName;
        }

        // Generate QR code data (simple format: payment://account/{accountNumber}?merchant={merchantId})
        var qrCodeData = $"payment://account/{accountNumber}?merchant={merchantId}&name={Uri.EscapeDataString(merchantName)}";

        var response = new QrCodeResponse
        {
            ToAccountNumber = accountNumber,
            MerchantId = merchantId,
            MerchantName = merchantName,
            QrCodeData = qrCodeData
        };

        return ApiResponse<QrCodeResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<TransactionResponse>> InitiatePaymentAsync(InitiatePaymentRequest request, int userId, CancellationToken cancellationToken = default)
    {
        // Get user's account
        var fromAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (fromAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("User account not found.", code: ErrorCodes.ResourceNotFound);

        // Validate destination account
        var toAccount = await db.Accounts
            .FirstOrDefaultAsync(a => a.AccountNumber == request.ToAccountNumber, cancellationToken);

        if (toAccount == null)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account not found.", code: ErrorCodes.DestinationAccountNotFound);

        if (toAccount.Status != AccountStatus.Active)
            return ApiResponse<TransactionResponse>.ErrorResponse("Destination account is not active.", code: ErrorCodes.AccountInactive);

        // Use existing transaction service to create transfer
        var transferRequest = new DTOs.requests.Transactions.CreateTransferRequest
        {
            FromAccountId = fromAccount.Id,
            ToAccountNumber = request.ToAccountNumber,
            Amount = request.Amount,
            Purpose = request.Purpose ?? "Payment via QR/NFC",
            IdempotencyKey = request.IdempotencyKey
        };

        return await transactionService.CreateTransferAsync(transferRequest, userId, cancellationToken);
    }
}
