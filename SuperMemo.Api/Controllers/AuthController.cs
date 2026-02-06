using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuperMemo.Api.Common;
using SuperMemo.Application.DTOs.requests.Auth;
using SuperMemo.Application.DTOs.responses.Auth;
using SuperMemo.Application.DTOs.responses.Common;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Api.Controllers;

[Route("auth")]
public class AuthController(IAuthService authService) : BaseController
{
    [HttpPost("register")]
    public async Task<ActionResult<RegisterResponse>> Register(RegisterRequest request, CancellationToken cancellationToken)
    {
        var result = await authService.Register(request, cancellationToken);
        return result.Success ? Created($"/User/{result.Data!.Id}", result) : BadRequest(result);
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
