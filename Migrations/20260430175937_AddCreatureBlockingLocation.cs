using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotAlone.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatureBlockingLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatureBlockingLocation",
                table: "GameSessions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatureBlockingLocation",
                table: "GameSessions");
        }
    }
}
