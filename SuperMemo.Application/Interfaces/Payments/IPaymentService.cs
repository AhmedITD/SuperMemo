using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payments;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Payments;

public interface IPaymentService
{
    Task<ApiResponse<PaymentResponse>> InitiateTopUpAsync(int userId, int accountId, decimal amount, string currency, string requestId, CancellationToken cancellationToken = default);
    Task<bool> ProcessWebhookAsync(string webhookPayload, string? signature, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentStatus>> VerifyPaymentStatusAsync(int paymentId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CancelPaymentAsync(int paymentId, int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<PaymentResponse>> GetPaymentAsync(int paymentId, int userId, CancellationToken cancellationToken = default);
}
