using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.Interfaces.Payments;
using System.Text;

namespace SuperMemo.Api.Controllers;

/// <summary>
/// Webhook endpoints for payment gateway callbacks.
/// These endpoints should NOT require authentication (gateway calls them).
/// </summary>
[Route("api/webhooks")]
public class WebhookController : BaseController
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IPaymentService paymentService, ILogger<WebhookController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    /// <summary>
    /// QiCard webhook endpoint for payment notifications.
    /// </summary>
    [HttpPost("qicard")]
    public async Task<IActionResult> QiCardWebhook(CancellationToken cancellationToken)
    {
        // Read raw body for signature verification
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        // Extract signature from header
        var signature = Request.Headers["X-QiCard-Signature"].FirstOrDefault() ??
                       Request.Headers["X-Signature"].FirstOrDefault() ??
                       Request.Headers["Signature"].FirstOrDefault();

        _logger.LogInformation("Received QiCard webhook. Signature present: {HasSignature}", !string.IsNullOrEmpty(signature));

        var processed = await _paymentService.ProcessWebhookAsync(rawBody, signature, cancellationToken);

        if (processed)
        {
            return Ok(new { success = true, message = "Webhook processed successfully" });
        }

        _logger.LogWarning("QiCard webhook processing failed. Signature valid: {HasSignature}", !string.IsNullOrEmpty(signature));
        return BadRequest(new { success = false, message = "Webhook processing failed" });
    }
}
