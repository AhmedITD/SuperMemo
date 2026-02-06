using System.Text.Json;

namespace SuperMemo.Application.DTOs.responses.QiCard;

public class QiCardResponse
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? PaymentId { get; set; }
    public JsonElement? Data { get; set; }
    public string? Error { get; set; }
    public string? ErrorCode { get; set; }
    public int? StatusCode { get; set; }
}
