using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NafasLand.Admin.Modules.Returns.Persistence.Migrations
{
    /// <inheritdoc />
    internal partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "returns");

            migrationBuilder.CreateTable(
                name: "ReturnRecords",
                schema: "returns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovalRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrderId = table.Column<long>(type: "bigint", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Shipping = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    OrderCreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OrderStatusesJson = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ItemsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ReturnDate = table.Column<DateOnly>(type: "date", nullable: false),
                    RegisteredByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ApprovedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReturnRecords", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_ApprovalRequestId",
                schema: "returns",
                table: "ReturnRecords",
                column: "ApprovalRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_ApprovedAt_Id",
                schema: "returns",
                table: "ReturnRecords",
                columns: new[] { "ApprovedAt", "Id" },
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_OrderId",
                schema: "returns",
                table: "ReturnRecords",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReturnRecords_RegisteredByUserId_ApprovedAt",
                schema: "returns",
                table: "ReturnRecords",
                columns: new[] { "RegisteredByUserId", "ApprovedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReturnRecords",
                schema: "returns");
        }
    }
}
