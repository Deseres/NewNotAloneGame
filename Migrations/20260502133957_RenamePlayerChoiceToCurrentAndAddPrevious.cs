using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotAlone.Migrations
{
    /// <inheritdoc />
    public partial class RenamePlayerChoiceToCurrentAndAddPrevious : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "LastPlayerChoice",
                table: "GameSessions",
                newName: "PreviousPlayerChoice");

            migrationBuilder.RenameColumn(
                name: "LastCreatureChoice",
                table: "GameSessions",
                newName: "PreviousCreatureChoice");

            migrationBuilder.AddColumn<int>(
                name: "CurrentCreatureChoice",
                table: "GameSessions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CurrentPlayerChoice",
                table: "GameSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentCreatureChoice",
                table: "GameSessions");

            migrationBuilder.DropColumn(
                name: "CurrentPlayerChoice",
                table: "GameSessions");

            migrationBuilder.RenameColumn(
                name: "PreviousPlayerChoice",
                table: "GameSessions",
                newName: "LastPlayerChoice");

            migrationBuilder.RenameColumn(
                name: "PreviousCreatureChoice",
                table: "GameSessions",
                newName: "LastCreatureChoice");
        }
    }
}
