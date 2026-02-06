using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Api.Models;
using SuperMemo.Application.DTOs.requests.Auth;
using SuperMemo.Application.DTOs.responses.Auth;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Application.Common;
using SuperMemo.Application.Interfaces.Storage;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Api.Controllers;

[Route("auth")]
public class AuthController(IAuthService authService, IStorageService storageService, IValidator<RegisterRequest> registerValidator) : BaseController
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp" };
    private const int MaxImageSizeBytes = 5 * 1024 * 1024; // 5 MB

    [HttpPost("register")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<RegisterResponse>> Register([FromForm] RegisterFormRequest form, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(form.FullName) || string.IsNullOrWhiteSpace(form.Phone)
            || string.IsNullOrWhiteSpace(form.Password) || string.IsNullOrWhiteSpace(form.VerificationCode))
            return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse("FullName, Phone, Password and VerificationCode are required."));

        string? userImageUrl = null;
        if (form.UserImage != null)
        {
            var urlResult = await UploadUserImageAsync(form.UserImage, cancellationToken);
            if (urlResult.Error != null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse(urlResult.Error));
            userImageUrl = urlResult.Url;
        }

        var icDoc = ParseDocumentJson<RegisterIcDocumentDto>(form.IcDocumentJson);
        var passportDoc = ParseDocumentJson<RegisterPassportDocumentDto>(form.PassportDocumentJson);
        var livingDoc = ParseDocumentJson<RegisterLivingIdentityDocumentDto>(form.LivingIdentityDocumentJson);

        if (form.IcDocumentImage != null)
        {
            var urlResult = await UploadKycImageAsync(form.IcDocumentImage, cancellationToken);
            if (urlResult.Error != null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse(urlResult.Error));
            if (icDoc == null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse("IcDocument metadata (JSON) is required when uploading an IC document image."));
            icDoc.ImageUrl = urlResult.Url;
        }
        if (form.PassportDocumentImage != null)
        {
            var urlResult = await UploadKycImageAsync(form.PassportDocumentImage, cancellationToken);
            if (urlResult.Error != null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse(urlResult.Error));
            if (passportDoc == null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse("PassportDocument metadata (JSON) is required when uploading a passport image."));
            passportDoc.ImageUrl = urlResult.Url;
        }
        if (form.LivingIdentityDocumentImage != null)
        {
            var urlResult = await UploadKycImageAsync(form.LivingIdentityDocumentImage, cancellationToken);
            if (urlResult.Error != null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse(urlResult.Error));
            if (livingDoc == null)
                return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse("LivingIdentityDocument metadata (JSON) is required when uploading a living identity image."));
            livingDoc.ImageUrl = urlResult.Url;
        }

        var request = new RegisterRequest
        {
            FullName = form.FullName.Trim(),
            Phone = form.Phone.Trim(),
            Password = form.Password,
            VerificationCode = form.VerificationCode,
            ImageUrl = userImageUrl,
            IcDocument = icDoc,
            PassportDocument = passportDoc,
            LivingIdentityDocument = livingDoc
        };
        var validation = await registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<RegisterResponse>.ErrorResponse("Validation failed.", validation.Errors.GroupBy(e => e.PropertyName).ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()!), ErrorCodes.ValidationFailed));
        var result = await authService.Register(request, cancellationToken);
        return result.Success ? Created($"/User/{result.Data!.Id}", result) : BadRequest(result);
    }

    private static T? ParseDocumentJson<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return null;
        }
    }

    private async Task<(string? Url, string? Error)> UploadKycImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return (null, "Image file is empty.");
        if (file.Length > MaxImageSizeBytes)
            return (null, $"Image size must not exceed {MaxImageSizeBytes / (1024 * 1024)} MB.");
        var contentType = file.ContentType ?? "image/jpeg";
        if (!AllowedImageTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return (null, "Only image/jpeg, image/png and image/webp are allowed.");
        var fileName = file.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "image.jpg";
        await using var stream = file.OpenReadStream();
        var url = await storageService.SaveKycImageAsync(stream, fileName, contentType, cancellationToken);
        return (url, null);
    }

    private async Task<(string? Url, string? Error)> UploadUserImageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            return (null, "Image file is empty.");
        if (file.Length > MaxImageSizeBytes)
            return (null, $"Image size must not exceed {MaxImageSizeBytes / (1024 * 1024)} MB.");
        var contentType = file.ContentType ?? "image/jpeg";
        if (!AllowedImageTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            return (null, "Only image/jpeg, image/png and image/webp are allowed.");
        var fileName = file.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
            fileName = "image.jpg";
        await using var stream = file.OpenReadStream();
        var url = await storageService.SaveUserImageAsync(stream, fileName, contentType, cancellationToken);
        return (url, null);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.Login(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.Refresh(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("logout")]
    public async Task<ActionResult<ApiResponse<object>>> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.Logout(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("logoutAllDevices")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> LogoutAllDevices(CancellationToken cancellationToken)
    {
        var result = await authService.LogoutAllDevices(cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpGet("Me")]
    public async Task<ActionResult<ApiResponse<User>>> Me(CancellationToken cancellationToken)
    {
        var response = await authService.Me(cancellationToken);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    /// <summary>Send OTP to a phone number (e.g. for verification or linking).</summary>
    [HttpPost("send-verification")]
    public async Task<ActionResult<ApiResponse<object>>> SendVerification([FromBody] SendVerificationRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await authService.SendVerificationAsync(request, ipAddress, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Request password reset: sends OTP to the account's phone number.</summary>
    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse<object>>> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var result = await authService.ForgotPasswordAsync(request, ipAddress, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Reset password using the OTP code sent to phone.</summary>
    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse<object>>> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.ResetPasswordAsync(request, cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
