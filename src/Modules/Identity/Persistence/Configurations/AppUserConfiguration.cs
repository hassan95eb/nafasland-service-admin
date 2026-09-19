using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Identity.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);

        builder.Property(user => user.Username).HasMaxLength(100).IsRequired();
        builder.HasIndex(user => user.Username).IsUnique();

        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.PasswordAlgorithm).HasMaxLength(50).IsRequired();
        builder.Property(user => user.TwoFactorSecret).HasMaxLength(200);

        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
