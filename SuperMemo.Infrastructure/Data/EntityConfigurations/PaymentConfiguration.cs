using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        builder.Property(x => x.PaymentGateway).IsRequired().HasMaxLength(50);
        builder.Property(x => x.GatewayPaymentId).HasMaxLength(100);
        builder.Property(x => x.RequestId).IsRequired().HasMaxLength(100);
        builder.Property(x => x.PaymentUrl).HasMaxLength(500);
        builder.Property(x => x.GatewayResponse).HasColumnType("text");
        builder.Property(x => x.WebhookData).HasColumnType("text");
        
        builder.HasOne(x => x.User)
            .WithMany(u => u.Payments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.Account)
            .WithMany(a => a.Payments)
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict);
            
        builder.HasOne(x => x.Transaction)
            .WithOne(t => t.Payment)
            .HasForeignKey<Payment>(x => x.TransactionId)
            .OnDelete(DeleteBehavior.SetNull);
        
        // Indexes for performance
        builder.HasIndex(x => x.RequestId).IsUnique();
        builder.HasIndex(x => x.GatewayPaymentId);
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasIndex(x => new { x.AccountId, x.Status });
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => x.Status);
    }
}
