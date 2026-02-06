using SuperMemo.Domain.Entities.Common;

namespace SuperMemo.Domain.Entities;

/// <summary>
/// Optional table for configurable fraud detection rules.
/// </summary>
public class FraudDetectionRule : BaseEntity
{
    public required string RuleName { get; set; }
    public required string RuleType { get; set; } // "amount_threshold", "velocity", "new_device", "new_card"
    public decimal? ThresholdValue { get; set; }
    public int? ThresholdCount { get; set; } // For velocity rules
    public TimeSpan? TimeWindow { get; set; } // For velocity rules
    public bool IsActive { get; set; } = true;
}
