using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Returns.Persistence.Configurations;

internal sealed class ReturnRecordConfiguration : IEntityTypeConfiguration<ReturnRecord>
{
    public void Configure(EntityTypeBuilder<ReturnRecord> builder)
    {
        builder.ToTable("ReturnRecords");
        builder.HasKey(record => record.Id);

        builder.Ignore(record => record.OrderStatuses);
        builder.Ignore(record => record.Items);

        builder.Property(record => record.CustomerName).HasMaxLength(300);
        builder.Property(record => record.Subtotal).HasPrecision(18, 2);
        builder.Property(record => record.Shipping).HasPrecision(18, 2);
        builder.Property(record => record.Discount).HasPrecision(18, 2);
        builder.Property(record => record.Tax).HasPrecision(18, 2);
        builder.Property(record => record.Total).HasPrecision(18, 2);
        builder.Property(record => record.OrderStatusesJson).HasMaxLength(1000).IsRequired();
        builder.Property(record => record.ItemsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(record => record.Reason).HasMaxLength(2000).IsRequired();

        // Both constraints are the real guard (rule 18): "read first, then write"
        // in the executor is only the friendly path; a concurrent or retried
        // insert is stopped here.
        builder.HasIndex(record => record.ApprovalRequestId).IsUnique();
        builder.HasIndex(record => record.OrderId).IsUnique();

        // Keyset pagination cursor (ADR-014): newest approval first, Id as tiebreaker.
        builder.HasIndex(record => new { record.ApprovedAt, record.Id }).IsDescending(true, true);
        builder.HasIndex(record => new { record.RegisteredByUserId, record.ApprovedAt });
    }
}
