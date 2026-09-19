using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Identity.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");
        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Key).HasMaxLength(150).IsRequired();
        builder.HasIndex(permission => permission.Key).IsUnique();

        builder.Property(permission => permission.ModuleName).HasMaxLength(100).IsRequired();
    }
}
