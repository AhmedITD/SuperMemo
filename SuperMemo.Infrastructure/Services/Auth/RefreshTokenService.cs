using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Auth;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Services.Auth;

public class RefreshTokenService(
    ISuperMemoDbContext dbContext,
    IConfiguration configuration) : IRefreshTokenService
{
    private const int TokenByteLength = 64;
    private const int HashLengthBytes = 32;

    private static int RefreshTokenExpiryDays(IConfiguration config)
    {
        var value = config["JwtSettings:RefreshTokenExpiryDays"];
        return int.TryParse(value, out var days) && days > 0 ? days : 7;
    }

    public async Task<(string RefreshToken, DateTime ExpiresAt)> CreateAsync(
        int userId,
        string? deviceClientId = null,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var plainToken = GenerateSecureToken();
        var hash = HashToken(plainToken);
        var expiresAt = DateTime.UtcNow.AddDays(RefreshTokenExpiryDays(configuration));

        var entity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            TokenHash = hash,
            UserId = userId,
            ExpiresAt = expiresAt,
            DeviceClientId = deviceClientId,
            IpAddress = ipAddress
        };

        dbContext.RefreshTokens.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return (plainToken, expiresAt);
    }

    public async Task<User?> ValidateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return null;

        var hash = HashToken(refreshToken);
        var entity = await dbContext.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(
                rt => rt.TokenHash == hash && rt.RevokedAt == null && rt.ExpiresAt > DateTime.UtcNow,
                cancellationToken);

        return entity?.User;
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var hash = HashToken(refreshToken);
        var entity = await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);
        if (entity == null) return;

        entity.RevokedAt = DateTime.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var t in tokens)
            t.RevokedAt = now;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[TokenByteLength];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    private static string HashToken(string plainToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(plainToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
