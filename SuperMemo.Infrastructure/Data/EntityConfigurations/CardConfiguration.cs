using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class CardConfiguration : IEntityTypeConfiguration<Card>
{
    public void Configure(EntityTypeBuilder<Card> builder)
    {
        builder.ToTable("Cards");
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.Number).IsUnique();
        builder.Property(x => x.Number).IsRequired().HasMaxLength(19);
        builder.Property(x => x.ScHashed).IsRequired();
        builder.HasOne(x => x.Account).WithMany(a => a.Cards).HasForeignKey(x => x.AccountId).OnDelete(DeleteBehavior.Restrict);
    }
}
