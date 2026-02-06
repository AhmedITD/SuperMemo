namespace SuperMemo.Application.DTOs.requests.Auth;

public class ForgotPasswordRequest
{
    /// <summary>Phone number of the account to send reset OTP to.</summary>
    public required string PhoneNumber { get; set; }
}
