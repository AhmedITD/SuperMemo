using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.DTOs.requests.QiCard;
using SuperMemo.Application.DTOs.responses.QiCard;
using SuperMemo.Application.Interfaces.Payments;

namespace SuperMemo.Infrastructure.Services;

public class QiCardService : IQiCardService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<QiCardService> _logger;
    private readonly string _baseUrl;
    private readonly string _xTerminalId;
    private readonly string _username;
    private readonly string _password;
    private readonly bool _isSandbox;

    public QiCardService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<QiCardService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _isSandbox = _configuration.GetValue<bool>("QiCard:Sandbox", false);

        if (_isSandbox)
        {
            _baseUrl = "https://uat-sandbox-3ds-api.qi.iq/api/v1";
            _xTerminalId = "237984";
            _username = "paymentgatewaytest";
            _password = "WHaNFE5C3qlChqNbAzH4";
        }
        else
        {
            _baseUrl = _configuration["QiCard:BaseUrl"] ?? "https://api-gate.qi.iq";
            _xTerminalId = _configuration["QiCard:XTerminalId"] ?? string.Empty;
            _username = _configuration["QiCard:Username"] ?? string.Empty;
            _password = _configuration["QiCard:Password"] ?? string.Empty;
        }
    }

    public async Task<QiCardResponse> InitiatePaymentAsync(QiCardPaymentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new
            {
                requestId = request.RequestId,
                amount = request.Amount,
                currency = request.Currency,
                locale = "en_US",
                finishPaymentUrl = request.FinishPaymentUrl,
                notificationUrl = request.NotificationUrl,
                customerInfo = new
                {
                    firstName = request.CustomerInfo?.FirstName ?? "Customer",
                    lastName = request.CustomerInfo?.LastName,
                    email = request.CustomerInfo?.Email
                },
                browserInfo = new
                {
                    browserAcceptHeader = request.BrowserInfo?.BrowserAcceptHeader ?? "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
                    browserIp = request.BrowserInfo?.BrowserIp,
                    browserJavaEnabled = request.BrowserInfo?.BrowserJavaEnabled ?? false,
                    browserLanguage = NormalizeBrowserLanguage(request.BrowserInfo?.BrowserLanguage ?? "en-US"),
                    browserColorDepth = "24",
                    browserScreenWidth = "1024",
                    browserScreenHeight = "768",
                    browserTZ = TimeZoneInfo.Local.GetUtcOffset(DateTime.Now).TotalMinutes.ToString(),
                    browserUserAgent = request.BrowserInfo?.BrowserUserAgent ?? "Mozilla/5.0"
                },
                additionalInfo = new
                {
                    description = request.Description ?? string.Empty
                }
            };

            _logger.LogInformation("QiCard payment payload: {Payload}", JsonSerializer.Serialize(payload));

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
            _httpClient.DefaultRequestHeaders.Add("X-Terminal-Id", _xTerminalId);

            var response = await _httpClient.PostAsync($"{_baseUrl}/payment", content, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                _logger.LogInformation("QiCard payment initiated successfully. Response: {Response}", responseBody);

                return new QiCardResponse
                {
                    Success = true,
                    Data = responseData,
                    PaymentUrl = responseData.TryGetProperty("formUrl", out var formUrl) 
                        ? GetStringValue(formUrl) 
                        : responseData.TryGetProperty("payment_url", out var paymentUrl) 
                            ? GetStringValue(paymentUrl) 
                            : null,
                    PaymentId = responseData.TryGetProperty("paymentId", out var paymentId) 
                        ? GetStringValue(paymentId) 
                        : responseData.TryGetProperty("id", out var id) 
                            ? GetStringValue(id) 
                            : null
                };
            }

            var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            var errorMessage = "Payment initiation failed";

            if (errorData.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("description", out var desc))
                    errorMessage = GetStringValue(desc) ?? errorMessage;
                else if (error.TryGetProperty("message", out var msg))
                    errorMessage = GetStringValue(msg) ?? errorMessage;
            }
            else if (errorData.TryGetProperty("message", out var msg))
            {
                errorMessage = GetStringValue(msg) ?? errorMessage;
            }

            _logger.LogError("QiCard payment initiation failed. Status: {Status}, Response: {Response}",
                response.StatusCode, responseBody);

            return new QiCardResponse
            {
                Success = false,
                Error = errorMessage,
                ErrorCode = errorData.TryGetProperty("error", out var err) && err.TryGetProperty("code", out var code)
                    ? GetStringValue(code)
                    : null,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QiCard payment exception");
            throw;
        }
    }

    public async Task<QiCardResponse> VerifyPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
            _httpClient.DefaultRequestHeaders.Add("X-Terminal-Id", _xTerminalId);

            var response = await _httpClient.GetAsync($"{_baseUrl}/payment/{paymentId}/status", cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseBody);

                _logger.LogInformation("QiCard payment verification successful. Response: {Response}", responseBody);

                return new QiCardResponse
                {
                    Success = true,
                    Data = responseData,
                    PaymentId = responseData.TryGetProperty("paymentId", out var pid)
                        ? GetStringValue(pid)
                        : responseData.TryGetProperty("id", out var id)
                            ? GetStringValue(id)
                            : paymentId
                };
            }
            
            _logger.LogError("QiCard payment verification failed. Status: {Status}, Response: {Response}",
                response.StatusCode, responseBody);
            
            var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            return new QiCardResponse
            {
                Success = false,
                Error = errorData.TryGetProperty("message", out var msg) 
                    ? GetStringValue(msg) ?? "Payment verification failed"
                    : "Payment verification failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QiCard payment verification exception");
            return new QiCardResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<QiCardResponse> CancelPaymentAsync(string paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_username}:{_password}"));
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
            _httpClient.DefaultRequestHeaders.Add("X-Terminal-Id", _xTerminalId);

            var response = await _httpClient.PostAsync($"{_baseUrl}/payment/{paymentId}/cancel", null, cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var responseData = JsonSerializer.Deserialize<JsonElement>(responseBody);
                
                _logger.LogInformation("QiCard payment cancellation successful. Response: {Response}", responseBody);
                
                return new QiCardResponse
                {
                    Success = true,
                    Data = responseData
                };
            }
            
            _logger.LogError("QiCard payment cancellation failed. Status: {Status}, Response: {Response}",
                response.StatusCode, responseBody);

            var errorData = JsonSerializer.Deserialize<JsonElement>(responseBody);
            return new QiCardResponse
            {
                Success = false,
                Error = errorData.TryGetProperty("message", out var msg) 
                    ? GetStringValue(msg) ?? "Payment cancellation failed"
                    : "Payment cancellation failed",
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QiCard payment cancellation exception");
            return new QiCardResponse
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public bool VerifyWebhookSignature(string rawBody, string? signatureFromHeader)
    {
        var secret = _configuration["QiCard:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("QiCard webhook secret is not configured");
            return false;
        }

        if (string.IsNullOrWhiteSpace(signatureFromHeader))
        {
            _logger.LogWarning("QiCard webhook signature is not provided");
            return false;
        }

        var payloadBytes = Encoding.UTF8.GetBytes(rawBody);
        var secretBytes = Encoding.UTF8.GetBytes(secret);
        var hash = System.Security.Cryptography.HMACSHA256.HashData(secretBytes, payloadBytes);

        var expectedHex = Convert.ToHexString(hash);
        var expectedBase64 = Convert.ToBase64String(hash);

        var provided = signatureFromHeader.Trim();
        if (provided.StartsWith("sha256=", StringComparison.OrdinalIgnoreCase))
            provided = provided.Substring(7).Trim();

        // Try both Base64 and hex comparison
        var isValid = expectedBase64.Equals(provided, StringComparison.OrdinalIgnoreCase) ||
                     expectedHex.Equals(provided, StringComparison.OrdinalIgnoreCase);

        if (!isValid)
        {
            _logger.LogWarning("QiCard webhook signature verification failed. Expected: {Expected}, Provided: {Provided}",
                expectedBase64, provided);
        }

        return isValid;
    }
    
    private static string NormalizeBrowserLanguage(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "en";
        }

        // Extract primary language code (before comma, semicolon, or space)
        var primaryLang = language.Split(',', ';', ' ')[0].Trim();
        
        // Truncate to max 8 characters if needed
        if (primaryLang.Length > 8)
        {
            primaryLang = primaryLang.Substring(0, 8);
        }

        // If empty after processing, default to "en"
        return string.IsNullOrWhiteSpace(primaryLang) ? "en" : primaryLang;
    }
    
    private static string? GetStringValue(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            return element.GetString();
        }
        else if (element.ValueKind == JsonValueKind.Number)
        {
            return element.GetRawText();
        }
        else if (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False)
        {
            return element.GetBoolean().ToString();
        }
        else if (element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        else
        {
            return element.GetRawText();
        }
    }
}
