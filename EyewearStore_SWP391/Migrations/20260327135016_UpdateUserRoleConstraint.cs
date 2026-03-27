using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearStore_SWP391.Migrations
{
    /// <inheritdoc />
    public partial class UpdateUserRoleConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update user role check constraint
            migrationBuilder.Sql("ALTER TABLE [dbo].[users] DROP CONSTRAINT IF EXISTS [CK_users_role];");
            migrationBuilder.Sql("ALTER TABLE [dbo].[users] ADD CONSTRAINT [CK_users_role] CHECK ([role] IN ('admin', 'manager', 'operational', 'customer', 'sale'));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE [dbo].[users] DROP CONSTRAINT IF EXISTS [CK_users_role];");
            migrationBuilder.Sql("ALTER TABLE [dbo].[users] ADD CONSTRAINT [CK_users_role] CHECK ([role] IN ('admin', 'manager', 'staff', 'customer'));");
        }
    }
}
