using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class MerchantAccountConfiguration : IEntityTypeConfiguration<MerchantAccount>
{
    public void Configure(EntityTypeBuilder<MerchantAccount> builder)
    {
        builder.ToTable("MerchantAccounts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MerchantId).IsRequired().HasMaxLength(50);
        builder.Property(x => x.MerchantName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.QrCodeData).HasMaxLength(500);
        builder.Property(x => x.NfcUrl).HasMaxLength(500);
        builder.HasOne(x => x.Account).WithMany().HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.MerchantId).IsUnique();
        builder.HasIndex(x => x.AccountId);
    }
}
