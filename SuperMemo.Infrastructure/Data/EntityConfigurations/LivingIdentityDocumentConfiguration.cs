using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class LivingIdentityDocumentConfiguration : IEntityTypeConfiguration<LivingIdentityDocument>
{
    public void Configure(EntityTypeBuilder<LivingIdentityDocument> builder)
    {
        builder.ToTable("LivingIdentityDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SerialNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FullFamilyName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.LivingLocation).IsRequired().HasMaxLength(200);
        builder.Property(x => x.FormNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.HasOne(x => x.User).WithMany(u => u.LivingIdentityDocuments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.UserId).IsUnique();
    }
}
