using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Sample.Persistence.Configurations;

internal sealed class PingRecordConfiguration : IEntityTypeConfiguration<PingRecord>
{
    public void Configure(EntityTypeBuilder<PingRecord> builder)
    {
        builder.ToTable("PingRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Message).HasMaxLength(200).IsRequired();
        builder.Property(record => record.CreatedAtUtc).IsRequired();
    }
}
