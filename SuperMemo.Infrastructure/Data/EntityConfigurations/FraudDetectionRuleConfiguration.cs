using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class FraudDetectionRuleConfiguration : IEntityTypeConfiguration<FraudDetectionRule>
{
    public void Configure(EntityTypeBuilder<FraudDetectionRule> builder)
    {
        builder.ToTable("FraudDetectionRules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RuleName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.RuleType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ThresholdValue).HasPrecision(18, 4);
        builder.HasIndex(x => x.RuleType);
        builder.HasIndex(x => x.IsActive);
    }
}
