using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotAlone.Migrations
{
    /// <inheritdoc />
    public partial class AddCompletedGameHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert a test user
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Username", "Email", "PasswordHash", "CreatedAt" },
                values: new object[] { new Guid("12345678-1234-1234-1234-123456789abc"), "TestPlayer", "test@example.com", "hashed_password", new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc) });

            // Insert the completed game history record
            migrationBuilder.InsertData(
                table: "GameHistories",
                columns: new[] { "Id", "UserId", "CompletedAt", "RoundsPlayed", "PlayerProgress", "CreatureProgress", "Result", "DurationSeconds" },
                values: new object[] { new Guid("87654321-4321-4321-4321-987654321def"), new Guid("12345678-1234-1234-1234-123456789abc"), new DateTime(2026, 4, 21, 0, 0, 0, 0, DateTimeKind.Utc), 4, 7, 5, "Win", 600 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Delete the game history first (due to foreign key)
            migrationBuilder.DeleteData(
                table: "GameHistories",
                keyColumn: "Id",
                keyValue: new Guid("87654321-4321-4321-4321-987654321def"));

            // Then delete the test user
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("12345678-1234-1234-1234-123456789abc"));
        }
    }
}
