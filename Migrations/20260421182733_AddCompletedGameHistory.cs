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
            // First, ensure a test user exists
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Username", "Email", "PasswordHash", "CreatedAt" },
                values: new object[] { Guid.Parse("12345678-1234-1234-1234-123456789abc"), "TestPlayer", "test@example.com", "hashed_password", DateTime.UtcNow });

            // Insert the completed game history record
            migrationBuilder.InsertData(
                table: "GameHistories",
                columns: new[] { "Id", "UserId", "CompletedAt", "RoundsPlayed", "PlayerProgress", "CreatureProgress", "Result", "DurationSeconds" },
                values: new object[] { Guid.NewGuid(), Guid.Parse("12345678-1234-1234-1234-123456789abc"), DateTime.UtcNow, 4, 7, 5, "Win", 600 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the test user and its game history
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: Guid.Parse("12345678-1234-1234-1234-123456789abc"));
        }
    }
}
