using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace NafasLand.Admin.Modules.Auditing.Persistence.Configurations;

internal sealed class AuditExportJobRecordConfiguration : IEntityTypeConfiguration<AuditExportJobRecord>
{
    public void Configure(EntityTypeBuilder<AuditExportJobRecord> builder)
    {
        builder.ToTable("AuditExportJobs");
        builder.HasKey(job => job.Id);

        builder.Property(job => job.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(job => job.Format).HasMaxLength(10).IsRequired();
        builder.Property(job => job.FilePath).HasMaxLength(1000);
        builder.Property(job => job.ErrorMessage).HasMaxLength(2000);
        builder.Property(job => job.CreatedAt).IsRequired();
    }
}
