namespace SuperMemo.Api.Models;

/// <summary>
/// Registration form: multipart/form-data with user fields, optional document JSON strings, and optional document image files.
/// Document metadata is sent as JSON strings; images are sent as files and stored externally; returned URLs are set on the DTOs.
/// </summary>
public class RegisterFormRequest
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Password { get; set; }
    public string? VerificationCode { get; set; }

    /// <summary>Optional user profile/avatar image file.</summary>
    public IFormFile? UserImage { get; set; }

    /// <summary>JSON string of <see cref="SuperMemo.Application.DTOs.requests.Auth.RegisterIcDocumentDto"/> (without ImageUrl).</summary>
    public string? IcDocumentJson { get; set; }
    /// <summary>JSON string of <see cref="SuperMemo.Application.DTOs.requests.Auth.RegisterPassportDocumentDto"/> (without ImageUrl).</summary>
    public string? PassportDocumentJson { get; set; }
    /// <summary>JSON string of <see cref="SuperMemo.Application.DTOs.requests.Auth.RegisterLivingIdentityDocumentDto"/> (without ImageUrl).</summary>
    public string? LivingIdentityDocumentJson { get; set; }

    public IFormFile? IcDocumentImage { get; set; }
    public IFormFile? PassportDocumentImage { get; set; }
    public IFormFile? LivingIdentityDocumentImage { get; set; }
}
