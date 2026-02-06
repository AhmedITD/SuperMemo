using SuperMemo.Domain.Entities;

namespace SuperMemo.Application.Interfaces.Auth;

public interface IRefreshTokenService
{
    Task<(string RefreshToken, DateTime ExpiresAt)> CreateAsync(
        int userId,
        string? deviceClientId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<User?> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default);
}
