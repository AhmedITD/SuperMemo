using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SuperMemo.Application.DTOs.responses.Otpiq;
using SuperMemo.Application.Interfaces;

namespace SuperMemo.Infrastructure.Services;

public class OtpiqService : IOtpiqService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OtpiqService> _logger;
    private readonly string _baseUrl;
    private readonly string _apiKey;

    public OtpiqService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OtpiqService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _baseUrl = _configuration["Otpiq:ApiUrl"] ?? "https://api.otpiq.com/api";
        _apiKey = _configuration["Otpiq:ApiKey"] ?? string.Empty;
    }

    public async Task<OtpiqResponse> SendVerificationCodeAsync(
        string phoneNumber,
        string verificationCode,
        string smsType = "verification",
        string provider = "whatsapp-sms")
    {
        try
        {
            var formattedPhone = FormatPhoneNumber(phoneNumber);

            var payload = new
            {
                phoneNumber = formattedPhone,
                smsType = smsType,
                provider = provider,
                verificationCode = verificationCode
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var response = await _httpClient.PostAsync($"{_baseUrl}/sms", content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<object>(responseBody);

                _logger.LogInformation("OTPIQ SMS sent successfully. Phone: {PhoneNumber}, Type: {SmsType}, Provider: {Provider}",
                    phoneNumber, smsType, provider);

                return new OtpiqResponse
                {
                    Success = true,
                    Message = "Verification code sent successfully",
                    Data = responseData,
                    StatusCode = (int)response.StatusCode
                };
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("OTPIQ SMS failed. Phone: {PhoneNumber}, Status: {Status}, Response: {Response}",
                formattedPhone, response.StatusCode, errorBody);

            return new OtpiqResponse
            {
                Success = false,
                Message = "Failed to send verification code",
                Error = errorBody,
                StatusCode = (int)response.StatusCode
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTPIQ service exception. Phone: {PhoneNumber}", phoneNumber);

            return new OtpiqResponse
            {
                Success = false,
                Message = "Failed to send verification code",
                Error = ex.Message,
                StatusCode = 500
            };
        }
    }

    public static string FormatPhoneNumber(string phoneNumber)
    {
        var cleaned = System.Text.RegularExpressions.Regex.Replace(phoneNumber, @"[^0-9+]", "");
        cleaned = cleaned.TrimStart('+');
        return cleaned;
    }
}
