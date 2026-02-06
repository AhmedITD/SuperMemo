using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Payments;
using SuperMemo.Application.Interfaces.Auth;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/payments")]
public class PaymentController(IPaymentInitiationService paymentService, ICurrentUser currentUser) : BaseController
{
    /// <summary>
    /// Generates QR code data for a merchant account.
    /// </summary>
    [HttpGet("qr/{accountNumber}")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Payments.QrCodeResponse>>> GenerateQrCode(
        string accountNumber, CancellationToken cancellationToken)
    {
        var result = await paymentService.GenerateQrCodeAsync(accountNumber, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Initiates a payment from NFC/QR scan.
    /// </summary>
    [HttpPost("initiate")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Transactions.TransactionResponse>>> InitiatePayment(
        [FromBody] InitiatePaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await paymentService.InitiatePaymentAsync(request, currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// NFC URL handler - redirects to payment page or returns JSON.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("pay")]
    public async Task<ActionResult<ApiResponse<Application.DTOs.responses.Payments.QrCodeResponse>>> Pay(
        [FromQuery] string to, [FromQuery] string? merchant, [FromQuery] string? name, CancellationToken cancellationToken)
    {
        // This endpoint can be used for NFC URL handling
        // Returns payment initiation data
        var result = await paymentService.GenerateQrCodeAsync(to, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
