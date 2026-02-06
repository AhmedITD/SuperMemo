using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.DTOs.responses.Payments;
using SuperMemo.Application.DTOs.responses.Transactions;

namespace SuperMemo.Application.Interfaces.Payments;

/// <summary>
/// Service for NFC/QR payment initiation.
/// </summary>
public interface IPaymentInitiationService
{
    /// <summary>
    /// Generates QR code data for a merchant account.
    /// </summary>
    Task<ApiResponse<QrCodeResponse>> GenerateQrCodeAsync(string accountNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Initiates a payment from NFC/QR scan.
    /// </summary>
    Task<ApiResponse<TransactionResponse>> InitiatePaymentAsync(InitiatePaymentRequest request, int userId, CancellationToken cancellationToken = default);
}
