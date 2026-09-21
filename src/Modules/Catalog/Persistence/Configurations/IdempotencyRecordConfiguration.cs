using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Catalog.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(record => record.Key);

        builder.Property(record => record.RequestHash).HasMaxLength(64).IsRequired();
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(record => record.ResponseJson).HasColumnType("nvarchar(max)");
        builder.Property(record => record.CreatedAt).IsRequired();
        builder.HasIndex(record => record.CreatedAt);
    }
}
