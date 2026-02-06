using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces.Auth;

public interface IJwtTokenGenerator
{
    /// <summary>Returns the JWT and its expiry time (UTC).</summary>
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
