namespace SuperMemo.Application.DTOs.responses.Payments;

public class QrCodeResponse
{
    public string Type { get; set; } = "payment";
    public required string ToAccountNumber { get; set; }
    public string? MerchantId { get; set; }
    public string? MerchantName { get; set; }
    public required string QrCodeData { get; set; }
}
