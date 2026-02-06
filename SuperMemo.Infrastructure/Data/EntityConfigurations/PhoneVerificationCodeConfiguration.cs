using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class PhoneVerificationCodeConfiguration : IEntityTypeConfiguration<PhoneVerificationCode>
{
    public void Configure(EntityTypeBuilder<PhoneVerificationCode> builder)
    {
        builder.ToTable("PhoneVerificationCodes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PhoneNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Code).IsRequired().HasMaxLength(6);
        builder.Property(x => x.ExpiresAt).IsRequired();
        builder.Property(x => x.VerifiedAt);
        builder.Property(x => x.IsUsed).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(45);
        builder.HasIndex(x => x.PhoneNumber);
        builder.HasIndex(x => new { x.PhoneNumber, x.Code, x.IsUsed });
    }
}
