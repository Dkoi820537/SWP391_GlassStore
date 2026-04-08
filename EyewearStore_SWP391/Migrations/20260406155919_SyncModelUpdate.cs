using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearStore_SWP391.Migrations
{
    /// <inheritdoc />
    public partial class SyncModelUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "cancellation_idempotency_key",
                table: "orders",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                table: "orders",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "deposit_amount",
                table: "orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "order_group_id",
                table: "orders",
                type: "nvarchar(36)",
                maxLength: 36,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "order_type",
                table: "orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Standard");

            migrationBuilder.AddColumn<string>(
                name: "payment_status",
                table: "orders",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "pending_balance",
                table: "orders",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "refund_amount",
                table: "orders",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "order_status_history",
                columns: table => new
                {
                    history_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    order_id = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    actor = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "System"),
                    note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "SYSDATETIME()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_status_history", x => x.history_id);
                    table.ForeignKey(
                        name: "FK_order_status_history_orders",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_orders_order_group",
                table: "orders",
                column: "order_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_created",
                table: "order_status_history",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_order_status_history_order",
                table: "order_status_history",
                column: "order_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_status_history");

            migrationBuilder.DropIndex(
                name: "IX_orders_order_group",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancellation_idempotency_key",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "deposit_amount",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_group_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_type",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "payment_status",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "pending_balance",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "refund_amount",
                table: "orders");
        }
    }
}
