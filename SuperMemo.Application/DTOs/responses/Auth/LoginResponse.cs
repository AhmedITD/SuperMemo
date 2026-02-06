namespace SuperMemo.Application.DTOs.responses.Auth;

public class LoginResponse
{
    public required string Token { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}
