using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearStore_SWP391.Migrations
{
    /// <inheritdoc />
    public partial class AddPrescriptionIdAndFeeToCartItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: add columns only if they don't exist (handles DBs partially updated by SQL script)
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('order_items') AND name = 'prescription_fee')
                    ALTER TABLE [order_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('lenses') AND name = 'prescription_fee')
                    ALTER TABLE [lenses] ADD [prescription_fee] decimal(18,2) NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('cart_items') AND name = 'prescription_fee')
                    ALTER TABLE [cart_items] ADD [prescription_fee] decimal(18,2) NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('cart_items') AND name = 'prescription_id')
                    ALTER TABLE [cart_items] ADD [prescription_id] int NULL;
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('cart_items') AND name = 'IX_cart_items_prescription_id')
                    CREATE INDEX [IX_cart_items_prescription_id] ON [cart_items]([prescription_id]);
                IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_cart_items_prescription')
                    ALTER TABLE [cart_items] ADD CONSTRAINT [FK_cart_items_prescription] FOREIGN KEY ([prescription_id]) REFERENCES [prescription_profiles]([prescription_id]) ON DELETE SET NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_cart_items_prescription",
                table: "cart_items");

            migrationBuilder.DropIndex(
                name: "IX_cart_items_prescription_id",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "prescription_fee",
                table: "order_items");

            migrationBuilder.DropColumn(
                name: "prescription_fee",
                table: "lenses");

            migrationBuilder.DropColumn(
                name: "prescription_fee",
                table: "cart_items");

            migrationBuilder.DropColumn(
                name: "prescription_id",
                table: "cart_items");
        }
    }
}
