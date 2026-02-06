using SuperMemo.Application.DTOs.requests.QiCard;
using SuperMemo.Application.DTOs.responses.QiCard;

namespace SuperMemo.Application.Interfaces.Payments;

public interface IQiCardService
{
    Task<QiCardResponse> InitiatePaymentAsync(QiCardPaymentRequest request, CancellationToken cancellationToken = default);
    Task<QiCardResponse> VerifyPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    Task<QiCardResponse> CancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default);
    bool VerifyWebhookSignature(string rawBody, string? signatureFromHeader);
}
