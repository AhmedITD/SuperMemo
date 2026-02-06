namespace SuperMemo.Application.DTOs.responses.Otpiq;

public class OtpiqResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
}
