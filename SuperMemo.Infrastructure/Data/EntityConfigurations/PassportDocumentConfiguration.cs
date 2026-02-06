using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class PassportDocumentConfiguration : IEntityTypeConfiguration<PassportDocument>
{
    public void Configure(EntityTypeBuilder<PassportDocument> builder)
    {
        builder.ToTable("PassportDocuments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PassportNumber).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Nationality).IsRequired().HasMaxLength(100);
        builder.Property(x => x.MotherFullName).IsRequired().HasMaxLength(200);
        builder.HasOne(x => x.User).WithMany(u => u.PassportDocuments).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
