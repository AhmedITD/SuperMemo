namespace SuperMemo.Application.DTOs.requests.Auth;

public class ResetPasswordRequest
{
    public required string PhoneNumber { get; set; }
    public required string Code { get; set; }
    public required string NewPassword { get; set; }
}
