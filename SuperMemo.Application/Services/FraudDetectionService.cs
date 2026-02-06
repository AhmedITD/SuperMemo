using Microsoft.EntityFrameworkCore;
using SuperMemo.Application.Interfaces;
using SuperMemo.Application.Interfaces.Fraud;
using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Services;

public class FraudDetectionService(ISuperMemoDbContext db) : IFraudDetectionService
{
    // Fraud detection rules (can be moved to config or database)
    private const decimal DailyTransactionLimit = 10000m;
    private const decimal MaxSingleTransfer = 5000m;
    private const int VelocityThreshold = 10; // transactions
    private static readonly TimeSpan VelocityTimeWindow = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan NewDeviceGracePeriod = TimeSpan.FromHours(24);
    private static readonly TimeSpan NewCardGracePeriod = TimeSpan.FromHours(24);

    public async Task<FraudDetectionResult> CalculateRiskScoreAsync(
        Transaction transaction,
        User user,
        Account account,
        DeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default)
    {
        var result = new FraudDetectionResult { RiskScore = 0 };

        // Check amount threshold
        if (await CheckAmountThresholdAsync(transaction.Amount, user.Id, cancellationToken))
        {
            result.RiskScore += 30;
            result.Reasons.Add("Amount threshold exceeded");
        }

        // Check velocity
        if (await CheckVelocityAsync(account.Id, VelocityTimeWindow, cancellationToken))
        {
            result.RiskScore += 25;
            result.Reasons.Add("Transaction velocity exceeded");
        }

        // Check new device (if device info provided)
        if (deviceInfo != null && await CheckNewDeviceAsync(user.Id, deviceInfo, cancellationToken))
        {
            result.RiskScore += 20;
            result.Reasons.Add("New device detected");
        }

        // Check new card (if we can determine which card is being used)
        // For simplicity, we'll check if any card on the account is new
        if (await CheckNewCardAsync(account.Id, null, cancellationToken))
        {
            result.RiskScore += 15;
            result.Reasons.Add("New card detected");
        }

        // Determine risk level
        result.RiskLevel = result.RiskScore switch
        {
            <= 30 => RiskLevel.Low,
            <= 70 => RiskLevel.Medium,
            _ => RiskLevel.High
        };

        return result;
    }

    public async Task<bool> CheckAmountThresholdAsync(decimal amount, int userId, CancellationToken cancellationToken = default)
    {
        // Check single transfer limit
        if (amount > MaxSingleTransfer)
            return true;

        // Check daily limit
        var today = DateTime.UtcNow.Date;
        var dailyTotal = await db.Transactions
            .Where(t => t.FromAccount!.UserId == userId
                && t.CreatedAt >= today
                && t.Status == TransactionStatus.Completed)
            .SumAsync(t => (decimal?)t.Amount, cancellationToken) ?? 0m;

        return (dailyTotal + amount) > DailyTransactionLimit;
    }

    public async Task<bool> CheckVelocityAsync(int accountId, TimeSpan timeWindow, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow - timeWindow;
        var count = await db.Transactions
            .CountAsync(t => t.FromAccountId == accountId
                && t.CreatedAt >= since
                && (t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.Sending || t.Status == TransactionStatus.Completed),
                cancellationToken);

        return count >= VelocityThreshold;
    }

    public async Task<bool> CheckNewDeviceAsync(int userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        // Simplified: In a real system, you'd track device history in a separate table
        // For now, we'll check if this is the user's first transaction today
        // This is a placeholder - implement proper device tracking as needed
        var today = DateTime.UtcNow.Date;
        var hasRecentTransaction = await db.Transactions
            .AnyAsync(t => t.FromAccount!.UserId == userId && t.CreatedAt >= today, cancellationToken);

        // If no recent transaction, consider it a new device
        // In production, implement proper device fingerprinting and tracking
        return !hasRecentTransaction;
    }

    public async Task<bool> CheckNewCardAsync(int accountId, int? cardId, CancellationToken cancellationToken = default)
    {
        var gracePeriodStart = DateTime.UtcNow - NewCardGracePeriod;
        
        // Check if any card on the account was created recently
        var hasNewCard = await db.Cards
            .AnyAsync(c => c.AccountId == accountId
                && c.CreatedAt >= gracePeriodStart
                && c.IsActive,
                cancellationToken);

        return hasNewCard;
    }
}
