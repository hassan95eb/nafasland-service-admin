using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NafasLand.Admin.Modules.Approvals.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "approvals");

            migrationBuilder.CreateTable(
                name: "ApprovalRequests",
                schema: "approvals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetEntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TargetEntityId = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewNote = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApprovalRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedAt_Id",
                schema: "approvals",
                table: "ApprovalRequests",
                columns: new[] { "RequestedAt", "Id" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_RequestedByUserId",
                schema: "approvals",
                table: "ApprovalRequests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_Status_RequestedAt",
                schema: "approvals",
                table: "ApprovalRequests",
                columns: new[] { "Status", "RequestedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_ApprovalRequests_TargetEntityType_TargetEntityId_Status",
                schema: "approvals",
                table: "ApprovalRequests",
                columns: new[] { "TargetEntityType", "TargetEntityId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApprovalRequests",
                schema: "approvals");
        }
    }
}
