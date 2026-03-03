using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WeatherDashboard.PreferencesApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_preferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_preferences", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "user_preferences",
                columns: new[] { "Id", "City", "CreatedAt", "DisplayOrder", "UserId" },
                values: new object[,]
                {
                    { 1, "Seattle", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 0, "demo-user" },
                    { 2, "Portland", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "demo-user" },
                    { 3, "Austin", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, "demo-user" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_UserId",
                table: "user_preferences",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_user_preferences_UserId_City",
                table: "user_preferences",
                columns: new[] { "UserId", "City" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_preferences");
        }
    }
}
