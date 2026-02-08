namespace SuperMemo.Api.Models;

/// <summary>
/// Registration form: multipart/form-data with user fields and optional user profile image.
/// </summary>
public class RegisterFormRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? VerificationCode { get; set; }

    /// <summary>Optional user profile/avatar image file.</summary>
    public IFormFile? UserImage { get; set; }
}
