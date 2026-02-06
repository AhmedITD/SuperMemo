using System.ComponentModel.DataAnnotations;

namespace SuperMemo.Application.DTOs.requests.Auth;

public class LogoutRequest
{
    [Required]
    public required string RefreshToken { get; set; }
}
