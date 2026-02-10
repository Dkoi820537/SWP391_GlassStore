using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EyewearStore_SWP391.Migrations
{
    /// <summary>
    /// Baseline migration: empty because the database was created via SQL scripts.
    /// This marks all existing tables as "already migrated" so future migrations
    /// only apply incremental changes.
    /// </summary>
    public partial class InitialBaseline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — all tables already exist in the database.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty — baseline cannot be rolled back.
        }
    }
}
