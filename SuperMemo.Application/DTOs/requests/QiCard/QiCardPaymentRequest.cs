namespace SuperMemo.Application.DTOs.requests.QiCard;

public class QiCardPaymentRequest
{
    public required string RequestId { get; set; }
    public decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string FinishPaymentUrl { get; set; }
    public required string NotificationUrl { get; set; }
    public CustomerInfoDto? CustomerInfo { get; set; }
    public BrowserInfoDto? BrowserInfo { get; set; }
    public string? Description { get; set; }
}

public class CustomerInfoDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}

public class BrowserInfoDto
{
    public string? BrowserAcceptHeader { get; set; }
    public string? BrowserIp { get; set; }
    public bool BrowserJavaEnabled { get; set; }
    public string? BrowserLanguage { get; set; }
    public string? BrowserColorDepth { get; set; }
    public string? BrowserScreenWidth { get; set; }
    public string? BrowserScreenHeight { get; set; }
    public string? BrowserTZ { get; set; }
    public string? BrowserUserAgent { get; set; }
}
