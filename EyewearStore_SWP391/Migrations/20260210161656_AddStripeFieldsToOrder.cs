using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearStore_SWP391.Migrations
{
    /// <summary>
    /// Adds Stripe Checkout Session ID and Payment Intent ID columns to the orders table,
    /// plus an index on stripe_session_id for fast webhook lookups.
    /// </summary>
    public partial class AddStripeFieldsToOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "stripe_session_id",
                table: "orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_payment_intent_id",
                table: "orders",
                type: "nvarchar(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_stripe_session",
                table: "orders",
                column: "stripe_session_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_orders_stripe_session",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "stripe_payment_intent_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "stripe_session_id",
                table: "orders");
        }
    }
}
