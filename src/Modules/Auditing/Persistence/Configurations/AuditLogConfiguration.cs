using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Auditing.Persistence.Configurations;

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(log => log.Id);

        builder.Property(log => log.CorrelationId).HasMaxLength(64).IsRequired();
        builder.Property(log => log.ActorRoleAtTime).HasMaxLength(200).IsRequired();
        builder.Property(log => log.Action).HasMaxLength(100).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(100);
        builder.Property(log => log.EntityId).HasMaxLength(200);
        builder.Property(log => log.BeforeJson).HasColumnType("nvarchar(max)");
        builder.Property(log => log.AfterJson).HasColumnType("nvarchar(max)");
        builder.Property(log => log.ChangedFields).HasColumnType("nvarchar(max)");
        builder.Property(log => log.Outcome).HasConversion<string>().HasMaxLength(10).IsRequired();
        builder.Property(log => log.FailureReason).HasMaxLength(2000);
        builder.Property(log => log.IpAddress).HasMaxLength(64);
        builder.Property(log => log.UserAgent).HasMaxLength(500);
        builder.Property(log => log.CreatedAt).IsRequired();
        builder.Property(log => log.ParentEntityType).HasMaxLength(100);
        builder.Property(log => log.ParentEntityId).HasMaxLength(200);

        // ADR-014's two required indexes, CreatedAt DESC as specified.
        builder.HasIndex(log => new { log.ActorUserId, log.CreatedAt }).IsDescending(false, true);
        builder.HasIndex(log => new { log.EntityType, log.EntityId, log.CreatedAt }).IsDescending(false, false, true);

        // Same shape, for per-entity history reaching child records (a product's variant changes).
        builder.HasIndex(log => new { log.ParentEntityType, log.ParentEntityId, log.CreatedAt }).IsDescending(false, false, true);
    }
}
