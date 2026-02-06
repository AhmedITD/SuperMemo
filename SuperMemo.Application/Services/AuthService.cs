using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperMemo.Application.Common;
using SuperMemo.Application.DTOs.requests.Auth;
using SuperMemo.Application.DTOs.responses.Auth;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class AuthService(
    ISuperMemoDbContext dbContext,
    IPasswordService passwordService,
    IJwtTokenGenerator tokenGenerator,
    IRefreshTokenService refreshTokenService,
    ICurrentUser currentUser,
    IOtpiqService otpiqService,
    IConfiguration configuration) : IAuthService
{
    public async Task<ApiResponse<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        if (await dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            return ApiResponse<RegisterResponse>.ErrorResponse("Email already exists.", code: ErrorCodes.EmailAlreadyExists);

        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = normalizedEmail,
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            PasswordHash = passwordService.HashPassword(request.Password),
            Role = UserRole.Customer
        };

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (refreshToken, refreshExpiresAt) = await refreshTokenService.CreateAsync(user.Id, cancellationToken: cancellationToken);
        var response = new RegisterResponse
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Phone = user.Phone,
            Token = tokenGenerator.GenerateToken(user),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };

        return ApiResponse<RegisterResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await ValidateUserCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user == null)
            return ApiResponse<LoginResponse>.ErrorResponse("Invalid credentials.", code: ErrorCodes.InvalidCredentials);

        var (refreshToken, refreshExpiresAt) = await refreshTokenService.CreateAsync(user.Id, cancellationToken: cancellationToken);
        var response = new LoginResponse
        {
            Token = tokenGenerator.GenerateToken(user),
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };

        return ApiResponse<LoginResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var user = await refreshTokenService.ValidateAsync(request.RefreshToken, cancellationToken);
        if (user == null)
            return ApiResponse<LoginResponse>.ErrorResponse("Invalid or expired refresh token.", code: ErrorCodes.InvalidRefreshToken);

        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        var (newRefreshToken, refreshExpiresAt) = await refreshTokenService.CreateAsync(user.Id, cancellationToken: cancellationToken);
        var response = new LoginResponse
        {
            Token = tokenGenerator.GenerateToken(user),
            RefreshToken = newRefreshToken,
            RefreshTokenExpiresAt = refreshExpiresAt
        };
        return ApiResponse<LoginResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<object>> Logout(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        await refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { }, "Logged out successfully.");
    }

    public async Task<ApiResponse<object>> LogoutAllDevices(CancellationToken cancellationToken = default)
    {
        await refreshTokenService.RevokeAllForUserAsync(currentUser.Id, cancellationToken);
        return ApiResponse<object>.SuccessResponse(new { }, "Logged out from all devices.");
    }

    public async Task<ApiResponse<User>> Me(CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == currentUser.Id, cancellationToken);
        if (user == null)
            return ApiResponse<User>.ErrorResponse("User not found.");
        return ApiResponse<User>.SuccessResponse(user);
    }

    public async Task<ApiResponse<object>> SendVerificationAsync(SendVerificationRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var phone = request.PhoneNumber?.Trim();
        if (string.IsNullOrEmpty(phone))
            return ApiResponse<object>.ErrorResponse("Phone number is required.");

        var code = new Random().Next(100000, 999999).ToString();
        var expiresInMinutes = configuration.GetValue("Otpiq:Code:ExpiresInMinutes", 10);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
        var provider = configuration["Otpiq:DefaultProvider"] ?? "whatsapp-sms";

        var verificationCode = new PhoneVerificationCode
        {
            PhoneNumber = phone,
            Code = code,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            IsUsed = false
        };
        dbContext.PhoneVerificationCodes.Add(verificationCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        var smsResult = await otpiqService.SendVerificationCodeAsync(phone, code, "verification", provider);

        if (!smsResult.Success)
            return ApiResponse<object>.SuccessResponse(new
            {
                message = "Verification code generated. SMS sending may have failed.",
                phone_number = phone,
                expires_in_minutes = expiresInMinutes,
                warning = "SMS service unavailable"
            });

        return ApiResponse<object>.SuccessResponse(new
        {
            message = "Verification code sent successfully",
            phone_number = phone,
            expires_in_minutes = expiresInMinutes
        });
    }

    public async Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var phone = request.PhoneNumber?.Trim();
        if (string.IsNullOrEmpty(phone))
            return ApiResponse<object>.ErrorResponse("Phone number is required.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
        if (user == null)
            return ApiResponse<object>.ErrorResponse("No account found with this phone number.");

        var code = new Random().Next(100000, 999999).ToString();
        var expiresInMinutes = configuration.GetValue("Otpiq:Code:ExpiresInMinutes", 10);
        var expiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes);
        var provider = configuration["Otpiq:DefaultProvider"] ?? "whatsapp-sms";

        var verificationCode = new PhoneVerificationCode
        {
            PhoneNumber = phone,
            Code = code,
            ExpiresAt = expiresAt,
            IpAddress = ipAddress,
            IsUsed = false
        };
        dbContext.PhoneVerificationCodes.Add(verificationCode);
        await dbContext.SaveChangesAsync(cancellationToken);

        var smsResult = await otpiqService.SendVerificationCodeAsync(phone, code, "verification", provider);

        if (!smsResult.Success)
            return ApiResponse<object>.SuccessResponse(new
            {
                message = "Verification code generated. SMS sending may have failed.",
                phone_number = phone,
                expires_in_minutes = expiresInMinutes,
                warning = "SMS service unavailable"
            });

        return ApiResponse<object>.SuccessResponse(new
        {
            message = "Verification code sent to your phone for password reset",
            phone_number = phone,
            expires_in_minutes = expiresInMinutes
        });
    }

    public async Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var phone = request.PhoneNumber?.Trim();
        if (string.IsNullOrEmpty(phone))
            return ApiResponse<object>.ErrorResponse("Phone number is required.");
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return ApiResponse<object>.ErrorResponse("New password is required.");

        var verificationCode = await dbContext.PhoneVerificationCodes
            .Where(v => v.PhoneNumber == phone && v.Code == request.Code && !v.IsUsed && v.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(v => v.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (verificationCode == null)
            return ApiResponse<object>.ErrorResponse("Invalid or expired verification code.");

        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Phone == phone, cancellationToken);
        if (user == null)
            return ApiResponse<object>.ErrorResponse("User not found.");

        user.PasswordHash = passwordService.HashPassword(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;
        verificationCode.IsUsed = true;
        verificationCode.VerifiedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        return ApiResponse<object>.SuccessResponse(new { message = "Password has been reset successfully." });
    }

    private async Task<User?> ValidateUserCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, cancellationToken);
        if (user == null)
            return null;
        return passwordService.VerifyPassword(password, user.PasswordHash) ? user : null;
    }
}
