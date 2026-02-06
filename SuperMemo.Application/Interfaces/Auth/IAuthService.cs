using SuperMemo.Application.DTOs.requests.Auth;
using SuperMemo.Application.DTOs.responses.Auth;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces.Auth;

public interface IAuthService
{
    Task<ApiResponse<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> Login(LoginRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<LoginResponse>> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> Logout(LogoutRequest request, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> LogoutAllDevices(CancellationToken cancellationToken = default);
    Task<ApiResponse<User>> Me(CancellationToken cancellationToken = default);

    /// <summary>Sends OTP to the given phone number (e.g. for phone verification or linking).</summary>
    Task<ApiResponse<object>> SendVerificationAsync(SendVerificationRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Sends OTP to the user's phone for password reset. User is looked up by phone.</summary>
    Task<ApiResponse<object>> ForgotPasswordAsync(ForgotPasswordRequest request, string? ipAddress, CancellationToken cancellationToken = default);

    /// <summary>Resets password using OTP code sent to phone.</summary>
    Task<ApiResponse<object>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
