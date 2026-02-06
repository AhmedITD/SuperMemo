using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Payments;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Interfaces.Payments;

namespace SuperMemo.Api.Controllers;

[Authorize]
[Route("api/payments")]
public class PaymentController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly ICurrentUser _currentUser;

    public PaymentController(IPaymentService paymentService, ICurrentUser currentUser)
    {
        _paymentService = paymentService;
        _currentUser = currentUser;
    }

    /// <summary>
    /// Initiate a wallet top-up via payment gateway.
    /// </summary>
    [HttpPost("top-up")]
    public async Task<ActionResult> TopUp([FromBody] TopUpRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentService.InitiateTopUpAsync(
            _currentUser.Id,
            request.AccountId,
            request.Amount,
            request.Currency,
            request.RequestId,
            cancellationToken);

        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Get payment details by ID.
    /// </summary>
    [HttpGet("{paymentId}")]
    public async Task<ActionResult> GetPayment(int paymentId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPaymentAsync(paymentId, _currentUser.Id, cancellationToken);
        
        if (!result.Success)
            return NotFound(result);
            
        return Ok(result);
    }

    /// <summary>
    /// Manually verify payment status with gateway.
    /// </summary>
    [HttpPost("{paymentId}/verify")]
    public async Task<ActionResult> VerifyPayment(int paymentId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.VerifyPaymentStatusAsync(paymentId, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>
    /// Cancel a pending payment.
    /// </summary>
    [HttpPost("{paymentId}/cancel")]
    public async Task<ActionResult> CancelPayment(int paymentId, CancellationToken cancellationToken)
    {
        var result = await _paymentService.CancelPaymentAsync(paymentId, _currentUser.Id, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
