namespace SuperMemo.Application.DTOs.requests.Auth;

public class RegisterRequest
{
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string Password { get; set; }
    public required string VerificationCode { get; set; }
    /// <summary>Optional profile image URL (from uploaded file during registration).</summary>
    public string? ImageUrl { get; set; }
    public RegisterIcDocumentDto? IcDocument { get; set; }
    public RegisterPassportDocumentDto? PassportDocument { get; set; }
    public RegisterLivingIdentityDocumentDto? LivingIdentityDocument { get; set; }
}
