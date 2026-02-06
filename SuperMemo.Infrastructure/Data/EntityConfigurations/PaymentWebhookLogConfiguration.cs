using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class PaymentWebhookLogConfiguration : IEntityTypeConfiguration<PaymentWebhookLog>
{
    public void Configure(EntityTypeBuilder<PaymentWebhookLog> builder)
    {
        builder.ToTable("PaymentWebhookLogs");
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.WebhookPayload).IsRequired().HasColumnType("text");
        builder.Property(x => x.Signature).HasMaxLength(500);
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        
        builder.HasOne(x => x.Payment)
            .WithMany(p => p.WebhookLogs)
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.CreatedAt);
        builder.HasIndex(x => new { x.PaymentId, x.Processed });
    }
}
