using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class IcDocumentConfiguration : IEntityTypeConfiguration<IcDocument>
{
    public void Configure(EntityTypeBuilder<IcDocument> builder)
    {
        builder.ToTable("IcDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IdentityCardNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.MotherFullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.BirthLocation).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.User).WithMany(u => u.IcDocuments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
