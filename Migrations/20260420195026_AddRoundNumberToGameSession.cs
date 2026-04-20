using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotAlone.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundNumberToGameSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoundNumber",
                table: "GameSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoundNumber",
                table: "GameSessions");
        }
    }
}
