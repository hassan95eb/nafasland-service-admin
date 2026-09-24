using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NafasLand.Admin.Modules.Auditing.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class AddAuditLogParentEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ParentEntityId",
                schema: "audit",
                table: "AuditLogs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ParentEntityType",
                schema: "audit",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_ParentEntityType_ParentEntityId_CreatedAt",
                schema: "audit",
                table: "AuditLogs",
                columns: new[] { "ParentEntityType", "ParentEntityId", "CreatedAt" },
                descending: new[] { false, false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_ParentEntityType_ParentEntityId_CreatedAt",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ParentEntityId",
                schema: "audit",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "ParentEntityType",
                schema: "audit",
                table: "AuditLogs");
        }
    }
}
