using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperMemo.Domain.Entities;

namespace SuperMemo.Infrastructure.Data.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FullName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.HasIndex(x => x.Phone).IsUnique();
        builder.Property(x => x.PasswordHash).IsRequired();
        builder.Property(x => x.Role).IsRequired();
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
    }
}
