using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Approvals.Persistence.Configurations;

internal sealed class ApprovalRequestConfiguration : IEntityTypeConfiguration<ApprovalRequest>
{
    public void Configure(EntityTypeBuilder<ApprovalRequest> builder)
    {
        builder.ToTable("ApprovalRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.RequestType).HasMaxLength(100).IsRequired();
        builder.Property(request => request.TargetEntityType).HasMaxLength(100).IsRequired();
        builder.Property(request => request.TargetEntityId).HasMaxLength(200).IsRequired();
        builder.Property(request => request.PayloadJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(request => request.SnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(request => request.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(request => request.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(request => request.ReviewNote).HasMaxLength(2000);
        builder.Property(request => request.ExecutionError).HasMaxLength(2000);
        builder.Property(request => request.RequestedAt).IsRequired();

        // Native SQL Server rowversion (ADR-019); EF checks it automatically on
        // every SaveChanges and throws DbUpdateConcurrencyException on a
        // conflicting write, which the handlers translate to 409 (ADR-010, rule 3).
        builder.Property(request => request.RowVersion).IsRowVersion();

        // Keyset pagination cursor, same shape as Auditing's ADR-014 index
        // (CreatedAt/RequestedAt DESC, Id as tiebreaker for same-tick rows).
        builder.HasIndex(request => new { request.RequestedAt, request.Id }).IsDescending(false, true);
        builder.HasIndex(request => new { request.Status, request.RequestedAt }).IsDescending(false, true);
        builder.HasIndex(request => new { request.TargetEntityType, request.TargetEntityId, request.Status });
        builder.HasIndex(request => request.RequestedByUserId);
    }
}
