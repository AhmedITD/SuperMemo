using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.DTOs.requests.QiCard;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payments;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Payments;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;
using SuperMemo.Domain.Exceptions;

namespace SuperMemo.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly ISuperMemoDbContext _db;
    private readonly IQiCardService _qiCardService;
    private readonly ILogger<PaymentService> _logger;
    private readonly IConfiguration _configuration;

    public PaymentService(
        ISuperMemoDbContext db,
        IQiCardService qiCardService,
        ILogger<PaymentService> logger,
        IConfiguration configuration)
    {
        _db = db;
        _qiCardService = qiCardService;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<ApiResponse<PaymentResponse>> InitiateTopUpAsync(
        int userId,
        int accountId,
        decimal amount,
        string currency,
        string requestId,
        CancellationToken cancellationToken = default)
    {
        // Validate user and account
        var account = await _db.Accounts
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.Id == accountId && a.UserId == userId, cancellationToken);

        if (account == null)
            return ApiResponse<PaymentResponse>.ErrorResponse("Account not found or access denied.", code: ErrorCodes.ResourceNotFound);

        if (account.Status != AccountStatus.Active)
            return ApiResponse<PaymentResponse>.ErrorResponse("Account is not active.", code: ErrorCodes.AccountInactive);

        if (account.User.ApprovalStatus != ApprovalStatus.Approved)
            return ApiResponse<PaymentResponse>.ErrorResponse("User is not approved for transactions.", code: ErrorCodes.UserNotApproved);

        if (amount <= 0)
            return ApiResponse<PaymentResponse>.ErrorResponse("Amount must be positive.");

        // Check idempotency
        var existingPayment = await _db.Payments
            .FirstOrDefaultAsync(p => p.RequestId == requestId, cancellationToken);

        if (existingPayment != null)
        {
            _logger.LogInformation("Payment with requestId {RequestId} already exists. Returning existing payment.", requestId);
            return ApiResponse<PaymentResponse>.SuccessResponse(Map(existingPayment));
        }

        // Create payment record
        var payment = new Payment
        {
            UserId = userId,
            AccountId = accountId,
            PaymentGateway = "QiCard",
            RequestId = requestId,
            Amount = amount,
            Currency = currency,
            Status = PaymentStatus.Pending
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            // Get user info for customer details
            var user = account.User;

            // Build QiCard request
            var qiCardRequest = new QiCardPaymentRequest
            {
                RequestId = requestId,
                Amount = amount,
                Currency = currency,
                FinishPaymentUrl = $"{GetBaseUrl()}/payment/callback",
                NotificationUrl = $"{GetBaseUrl()}/api/webhooks/qicard",
                CustomerInfo = new CustomerInfoDto
                {
                    FirstName = user.FullName?.Split(' ').FirstOrDefault(),
                    LastName = user.FullName?.Split(' ').Skip(1).FirstOrDefault()?.Trim(),
                    Email = null // Add email to User entity if needed
                },
                Description = $"Wallet top-up for account {account.AccountNumber}"
            };

            // Call QiCard service
            var qiCardResponse = await _qiCardService.InitiatePaymentAsync(qiCardRequest, cancellationToken);

            if (qiCardResponse.Success && !string.IsNullOrEmpty(qiCardResponse.PaymentUrl))
            {
                // Update payment with gateway response
                payment.GatewayPaymentId = qiCardResponse.PaymentId;
                payment.PaymentUrl = qiCardResponse.PaymentUrl;
                payment.GatewayResponse = qiCardResponse.Data?.ToString() ?? JsonSerializer.Serialize(qiCardResponse);
                payment.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment {PaymentId} initiated successfully. Gateway payment ID: {GatewayPaymentId}",
                    payment.Id, payment.GatewayPaymentId);

                return ApiResponse<PaymentResponse>.SuccessResponse(Map(payment));
            }
            else
            {
                // Payment initiation failed
                payment.Status = PaymentStatus.Failed;
                payment.GatewayResponse = qiCardResponse.Error ?? JsonSerializer.Serialize(qiCardResponse);
                payment.UpdatedAt = DateTime.UtcNow;

                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogError("Payment {PaymentId} initiation failed. Error: {Error}",
                    payment.Id, qiCardResponse.Error);

                return ApiResponse<PaymentResponse>.ErrorResponse(
                    qiCardResponse.Error ?? "Payment initiation failed.",
                    code: ErrorCodes.PaymentInitiationFailed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during payment initiation for payment {PaymentId}", payment.Id);
            
            payment.Status = PaymentStatus.Failed;
            payment.GatewayResponse = ex.Message;
            payment.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(cancellationToken);

            return ApiResponse<PaymentResponse>.ErrorResponse("An error occurred while initiating payment.", code: ErrorCodes.InternalError);
        }
    }

    public async Task<bool> ProcessWebhookAsync(string webhookPayload, string? signature, CancellationToken cancellationToken = default)
    {
        // Verify signature
        if (!_qiCardService.VerifyWebhookSignature(webhookPayload, signature))
        {
            _logger.LogWarning("Webhook signature verification failed. Payload: {Payload}", webhookPayload);
            return false;
        }

        try
        {
            var webhookData = JsonSerializer.Deserialize<JsonElement>(webhookPayload);
            
            // Extract payment ID from webhook
            var gatewayPaymentId = webhookData.TryGetProperty("paymentId", out var pid)
                ? pid.GetString()
                : webhookData.TryGetProperty("id", out var id)
                    ? id.GetString()
                    : null;

            if (string.IsNullOrEmpty(gatewayPaymentId))
            {
                _logger.LogWarning("Webhook payload does not contain payment ID. Payload: {Payload}", webhookPayload);
                return false;
            }

            // Find payment by gateway payment ID
            var payment = await _db.Payments
                .Include(p => p.Account)
                .FirstOrDefaultAsync(p => p.GatewayPaymentId == gatewayPaymentId, cancellationToken);

            if (payment == null)
            {
                _logger.LogWarning("Payment not found for gateway payment ID: {GatewayPaymentId}", gatewayPaymentId);
                return false;
            }

            // Check if webhook already processed (idempotency)
            if (payment.WebhookReceived)
            {
                _logger.LogInformation("Webhook already processed for payment {PaymentId}. Skipping.", payment.Id);
                return true;
            }

            // Log webhook
            var webhookLog = new PaymentWebhookLog
            {
                PaymentId = payment.Id,
                WebhookPayload = webhookPayload,
                Signature = signature,
                SignatureValid = true
            };

            _db.PaymentWebhookLogs.Add(webhookLog);

            // Extract status from webhook
            var status = ExtractStatusFromWebhook(webhookData);
            var amount = ExtractAmountFromWebhook(webhookData);

            // Verify amount matches
            if (amount.HasValue && Math.Abs(amount.Value - payment.Amount) > 0.01m)
            {
                _logger.LogWarning("Payment amount mismatch. Expected: {Expected}, Received: {Received}",
                    payment.Amount, amount.Value);
                
                webhookLog.ErrorMessage = $"Amount mismatch. Expected: {payment.Amount}, Received: {amount.Value}";
                webhookLog.Processed = false;
                await _db.SaveChangesAsync(cancellationToken);
                return false;
            }

            // Process based on status
            if (status == "SUCCESS" || status == "COMPLETED" || status == "SUCCESSFUL")
            {
                await ProcessSuccessfulPayment(payment, webhookData, webhookLog, cancellationToken);
            }
            else if (status == "FAILED" || status == "FAILURE")
            {
                payment.Status = PaymentStatus.Failed;
                payment.WebhookReceived = true;
                payment.WebhookData = webhookPayload;
                payment.UpdatedAt = DateTime.UtcNow;
                
                webhookLog.Processed = true;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                
                await _db.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Payment {PaymentId} marked as failed via webhook.", payment.Id);
            }
            else if (status == "CANCELLED" || status == "CANCELED")
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.WebhookReceived = true;
                payment.WebhookData = webhookPayload;
                payment.UpdatedAt = DateTime.UtcNow;
                
                webhookLog.Processed = true;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                
                await _db.SaveChangesAsync(cancellationToken);
                
                _logger.LogInformation("Payment {PaymentId} marked as cancelled via webhook.", payment.Id);
            }
            else
            {
                _logger.LogWarning("Unknown payment status in webhook: {Status}. Payment ID: {PaymentId}", status, payment.Id);
                webhookLog.ErrorMessage = $"Unknown status: {status}";
                webhookLog.Processed = false;
                await _db.SaveChangesAsync(cancellationToken);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception processing webhook. Payload: {Payload}", webhookPayload);
            return false;
        }
    }

    private async Task ProcessSuccessfulPayment(
        Payment payment,
        JsonElement webhookData,
        PaymentWebhookLog webhookLog,
        CancellationToken cancellationToken)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Check if transaction already created
            if (payment.TransactionId.HasValue)
            {
                _logger.LogInformation("Transaction already exists for payment {PaymentId}. Skipping transaction creation.", payment.Id);
                payment.WebhookReceived = true;
                payment.WebhookData = webhookData.ToString();
                payment.UpdatedAt = DateTime.UtcNow;
                webhookLog.Processed = true;
                webhookLog.ProcessedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return;
            }

            // Create transaction record
            var account = payment.Account;
            var topUpTransaction = new Transaction
            {
                FromAccountId = account.Id, // For TOP_UP, from_account is the same account
                ToAccountNumber = account.AccountNumber,
                Amount = payment.Amount,
                TransactionType = TransactionType.Credit,
                Status = TransactionStatus.Completed,
                Category = TransactionCategory.TopUp,
                Purpose = $"Wallet top-up via {payment.PaymentGateway}",
                IdempotencyKey = payment.RequestId,
                PaymentId = payment.Id,
                StatusChangedAt = DateTime.UtcNow
            };

            _db.Transactions.Add(topUpTransaction);
            await _db.SaveChangesAsync(cancellationToken);

            // Update account balance
            account.Balance += payment.Amount;
            account.UpdatedAt = DateTime.UtcNow;

            // Update payment
            payment.Status = PaymentStatus.Completed;
            payment.TransactionId = topUpTransaction.Id;
            payment.WebhookReceived = true;
            payment.WebhookData = webhookData.ToString();
            payment.UpdatedAt = DateTime.UtcNow;

            // Update webhook log
            webhookLog.Processed = true;
            webhookLog.ProcessedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Payment {PaymentId} processed successfully. Transaction {TransactionId} created. Balance updated to {Balance}",
                payment.Id, topUpTransaction.Id, account.Balance);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Exception processing successful payment {PaymentId}", payment.Id);
            webhookLog.ErrorMessage = ex.Message;
            webhookLog.Processed = false;
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<ApiResponse<PaymentStatus>> VerifyPaymentStatusAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .Include(p => p.Account)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        if (payment == null)
            return ApiResponse<PaymentStatus>.ErrorResponse("Payment not found.", code: ErrorCodes.ResourceNotFound);

        if (string.IsNullOrEmpty(payment.GatewayPaymentId))
            return ApiResponse<PaymentStatus>.ErrorResponse("Payment gateway ID not found.");

        try
        {
            var qiCardResponse = await _qiCardService.VerifyPaymentAsync(payment.GatewayPaymentId, cancellationToken);

            if (qiCardResponse.Success && qiCardResponse.Data.HasValue)
            {
                var status = ExtractStatusFromWebhook(qiCardResponse.Data.Value);
                
                // Update payment status if changed
                var newStatus = MapStatus(status);
                if (newStatus != payment.Status)
                {
                    payment.Status = newStatus;
                    payment.UpdatedAt = DateTime.UtcNow;
                    
                    // If payment completed but transaction not created, create it
                    if (newStatus == PaymentStatus.Completed && !payment.TransactionId.HasValue)
                    {
                        var webhookLog = new PaymentWebhookLog 
                        { 
                            PaymentId = payment.Id,
                            WebhookPayload = qiCardResponse.Data.Value.ToString()
                        };
                        await ProcessSuccessfulPayment(payment, qiCardResponse.Data.Value, webhookLog, cancellationToken);
                    }
                    
                    await _db.SaveChangesAsync(cancellationToken);
                }

                return ApiResponse<PaymentStatus>.SuccessResponse(payment.Status);
            }

            return ApiResponse<PaymentStatus>.ErrorResponse(
                qiCardResponse.Error ?? "Payment verification failed.",
                code: ErrorCodes.PaymentVerificationFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception verifying payment {PaymentId}", paymentId);
            return ApiResponse<PaymentStatus>.ErrorResponse("An error occurred while verifying payment.", code: ErrorCodes.InternalError);
        }
    }

    public async Task<ApiResponse<bool>> CancelPaymentAsync(int paymentId, int userId, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, cancellationToken);

        if (payment == null)
            return ApiResponse<bool>.ErrorResponse("Payment not found or access denied.", code: ErrorCodes.ResourceNotFound);

        if (payment.Status != PaymentStatus.Pending)
            return ApiResponse<bool>.ErrorResponse("Only pending payments can be cancelled.", code: ErrorCodes.InvalidOperation);

        if (string.IsNullOrEmpty(payment.GatewayPaymentId))
            return ApiResponse<bool>.ErrorResponse("Payment gateway ID not found.");

        try
        {
            var qiCardResponse = await _qiCardService.CancelPaymentAsync(payment.GatewayPaymentId, cancellationToken);

            if (qiCardResponse.Success)
            {
                payment.Status = PaymentStatus.Cancelled;
                payment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation("Payment {PaymentId} cancelled successfully.", paymentId);
                return ApiResponse<bool>.SuccessResponse(true);
            }

            return ApiResponse<bool>.ErrorResponse(
                qiCardResponse.Error ?? "Payment cancellation failed.",
                code: ErrorCodes.PaymentCancellationFailed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception cancelling payment {PaymentId}", paymentId);
            return ApiResponse<bool>.ErrorResponse("An error occurred while cancelling payment.", code: ErrorCodes.InternalError);
        }
    }

    public async Task<ApiResponse<PaymentResponse>> GetPaymentAsync(int paymentId, int userId, CancellationToken cancellationToken = default)
    {
        var payment = await _db.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId && p.UserId == userId, cancellationToken);

        if (payment == null)
            return ApiResponse<PaymentResponse>.ErrorResponse("Payment not found or access denied.", code: ErrorCodes.ResourceNotFound);

        return ApiResponse<PaymentResponse>.SuccessResponse(Map(payment));
    }

    private static PaymentResponse Map(Payment payment)
    {
        return new PaymentResponse
        {
            Id = payment.Id,
            PaymentGateway = payment.PaymentGateway,
            GatewayPaymentId = payment.GatewayPaymentId,
            RequestId = payment.RequestId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            PaymentUrl = payment.PaymentUrl,
            TransactionId = payment.TransactionId,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    private static string ExtractStatusFromWebhook(JsonElement webhookData)
    {
        if (webhookData.TryGetProperty("status", out var status))
            return status.GetString() ?? "UNKNOWN";
        
        if (webhookData.TryGetProperty("paymentStatus", out var paymentStatus))
            return paymentStatus.GetString() ?? "UNKNOWN";
        
        return "UNKNOWN";
    }

    private static decimal? ExtractAmountFromWebhook(JsonElement webhookData)
    {
        if (webhookData.TryGetProperty("amount", out var amount))
        {
            if (amount.ValueKind == JsonValueKind.Number)
                return amount.GetDecimal();
            if (amount.ValueKind == JsonValueKind.String && decimal.TryParse(amount.GetString(), out var parsed))
                return parsed;
        }
        return null;
    }

    private static PaymentStatus MapStatus(string status)
    {
        return status.ToUpperInvariant() switch
        {
            "SUCCESS" or "COMPLETED" or "SUCCESSFUL" => PaymentStatus.Completed,
            "FAILED" or "FAILURE" => PaymentStatus.Failed,
            "CANCELLED" or "CANCELED" => PaymentStatus.Cancelled,
            _ => PaymentStatus.Pending
        };
    }

    private string GetBaseUrl()
    {
        return _configuration["BaseUrl"] ?? 
               _configuration["AppSettings:BaseUrl"] ?? 
               Environment.GetEnvironmentVariable("BASE_URL") ?? 
               "https://yourapp.com";
    }
}
