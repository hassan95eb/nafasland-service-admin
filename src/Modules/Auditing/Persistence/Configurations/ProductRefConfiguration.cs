using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Auditing.Persistence.Configurations;

internal sealed class ProductRefConfiguration : IEntityTypeConfiguration<ProductRef>
{
    public void Configure(EntityTypeBuilder<ProductRef> builder)
    {
        builder.ToTable("ProductRefs");
        builder.Property(productRef => productRef.ExternalProductId).HasMaxLength(100);
        builder.HasKey(productRef => productRef.ExternalProductId);

        builder.Property(productRef => productRef.LastKnownTitle).HasMaxLength(500).IsRequired();
        builder.Property(productRef => productRef.LastSyncedAt).IsRequired();
    }
}
