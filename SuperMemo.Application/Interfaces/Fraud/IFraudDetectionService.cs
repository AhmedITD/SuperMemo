using SuperMemo.Domain.Entities;
using SuperMemo.Domain.Enums;

namespace SuperMemo.Application.Interfaces.Fraud;

/// <summary>
/// Service for fraud detection and risk scoring.
/// </summary>
public interface IFraudDetectionService
{
    /// <summary>
    /// Calculates risk score and level for a transaction.
    /// </summary>
    Task<FraudDetectionResult> CalculateRiskScoreAsync(
        Transaction transaction,
        User user,
        Account account,
        DeviceInfo? deviceInfo = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if amount exceeds daily or maximum threshold.
    /// </summary>
    Task<bool> CheckAmountThresholdAsync(decimal amount, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if transaction velocity is too high (too many transactions in short time).
    /// </summary>
    Task<bool> CheckVelocityAsync(int accountId, TimeSpan timeWindow, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if device is new (not recognized for this user).
    /// </summary>
    Task<bool> CheckNewDeviceAsync(int userId, DeviceInfo deviceInfo, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if card is new (linked less than grace period ago).
    /// </summary>
    Task<bool> CheckNewCardAsync(int accountId, int? cardId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of fraud detection analysis.
/// </summary>
public class FraudDetectionResult
{
    public int RiskScore { get; set; } // 0-100
    public RiskLevel RiskLevel { get; set; }
    public List<string> Reasons { get; set; } = new();
}

/// <summary>
/// Device information for fraud detection.
/// </summary>
public class DeviceInfo
{
    public string? DeviceId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
