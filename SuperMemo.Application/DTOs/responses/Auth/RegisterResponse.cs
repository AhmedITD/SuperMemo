namespace SuperMemo.Application.DTOs.responses.Auth;

public class RegisterResponse
{
    public int Id { get; set; }
    public required string FullName { get; set; }
    public required string Phone { get; set; }
    public required string Token { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public required string RefreshToken { get; set; }
    public DateTime RefreshTokenExpiresAt { get; set; }
}
